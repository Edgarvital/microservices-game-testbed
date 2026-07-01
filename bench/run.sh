#!/usr/bin/env bash
#
# Orquestrador do benchmark de overhead do mTLS (feature flag).
# Roda os cenarios A/B/C, N repeticoes cada, coletando latencia, throughput e
# uso de recursos. NAO inventa dados: executa carga real contra o cluster.
#
# Cenarios:
#   A  - sem mesh          (namespace sem injecao de sidecar)
#   B  - mesh, mTLS OFF    (sidecar + PeerAuthentication DISABLE)
#   C  - mesh, mTLS ON     (sidecar + PeerAuthentication STRICT + excecao do WS)
#
# Pre-requisitos: cluster Kubernetes com Istio, servicos ja implantados
# (deploy/k8s/*), metrics-server (para kubectl top) e um inventario de teste
# semeado (bench/seed/seed-inventory.sql). Ver bench/README.md.
#
# Uso:
#   bash bench/run.sh
#   REPS=10 GHZ_N=50000 SCENARIOS="B C" bash bench/run.sh
#
set -euo pipefail

# ----- parametros (override por env) -----
NAMESPACE=${NAMESPACE:-onlinegame}
REPS=${REPS:-5}
SCENARIOS=${SCENARIOS:-A B C}
# Apenas os apps sao reiniciados ao trocar de cenario. Os bancos e o Redis NAO,
# para nao perder dados (Postgres efemero) nem sofrer com boot ordering.
APP_DEPLOYMENTS=${APP_DEPLOYMENTS:-"auth-service inventory-service matchmaking-service battle-service"}
SETTLE_SECONDS=${SETTLE_SECONDS:-20}          # espera pos-rollout p/ estabilizar
JOB_TIMEOUT=${JOB_TIMEOUT:-600}               # timeout por Job (s)
PLAYER_ID=${PLAYER_ID:-11111111-1111-1111-1111-111111111111}

# ghz (gRPC battle -> inventory)
GHZ_IMAGE=${GHZ_IMAGE:-ghcr.io/bojand/ghz:latest}
GHZ_N=${GHZ_N:-20000}                          # total de requests
GHZ_C=${GHZ_C:-50}                             # concorrencia
GHZ_CONN=${GHZ_CONN:-10}                        # conexoes
GHZ_SKIP=${GHZ_SKIP:-2000}                      # warm-up descartado

# k6 (REST matchmaking -> inventory)
K6_IMAGE=${K6_IMAGE:-grafana/k6:latest}
K6_VUS=${K6_VUS:-50}
K6_DURATION=${K6_DURATION:-60s}

RUN_K6=${RUN_K6:-1}                             # 1 = tambem roda o REST
RUN_GHZ=${RUN_GHZ:-1}

# ----- caminhos -----
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
STAMP=$(date -u +%Y%m%d-%H%M%S)
OUT="$SCRIPT_DIR/results/$STAMP"
mkdir -p "$OUT"

log() { echo "[$(date -u +%H:%M:%S)] $*"; }

require() { command -v "$1" >/dev/null 2>&1 || { echo "ERRO: '$1' nao encontrado no PATH." >&2; exit 1; }; }
require kubectl
require envsubst

kubectl cluster-info >/dev/null 2>&1 || { echo "ERRO: nenhum cluster Kubernetes acessivel." >&2; exit 1; }

# Metadados do ambiente (essencial para replicabilidade do artigo)
{
  echo "run_stamp_utc: $STAMP"
  echo "namespace: $NAMESPACE"
  echo "reps: $REPS"
  echo "scenarios: $SCENARIOS"
  echo "ghz: image=$GHZ_IMAGE n=$GHZ_N c=$GHZ_C conn=$GHZ_CONN skipFirst=$GHZ_SKIP"
  echo "k6: image=$K6_IMAGE vus=$K6_VUS duration=$K6_DURATION"
  echo "player_id: $PLAYER_ID"
  echo "kubectl_context: $(kubectl config current-context 2>/dev/null || echo n/a)"
  echo "--- istio ---"
  istioctl version 2>/dev/null || echo "istioctl: n/a"
  echo "--- nodes ---"
  kubectl get nodes -o wide 2>/dev/null || true
} > "$OUT/environment.txt"
log "Metadados salvos em $OUT/environment.txt"

# ConfigMaps de suporte (proto p/ ghz, script p/ k6)
kubectl create configmap bench-proto -n "$NAMESPACE" \
  --from-file=player_inventory.proto="$REPO_ROOT/proto/player_inventory.proto" \
  --dry-run=client -o yaml | kubectl apply -f - >/dev/null
kubectl create configmap bench-k6-script -n "$NAMESPACE" \
  --from-file=rest.js="$SCRIPT_DIR/k6/rest.js" \
  --dry-run=client -o yaml | kubectl apply -f - >/dev/null

# ----- helpers de cenario -----
configure_mesh() {
  local scenario="$1"
  case "$scenario" in
    A)
      log "Cenario A: desabilitando injecao de sidecar (sem mesh)"
      kubectl label namespace "$NAMESPACE" istio-injection- --overwrite >/dev/null 2>&1 || true
      kubectl delete peerauthentication --all -n "$NAMESPACE" --ignore-not-found >/dev/null 2>&1 || true
      export GHZ_SIDECAR_ANNOTATION='sidecar.istio.io/inject: "false"'
      export K6_SIDECAR_ANNOTATION='sidecar.istio.io/inject: "false"'
      ;;
    B)
      log "Cenario B: mesh com mTLS DISABLE"
      kubectl label namespace "$NAMESPACE" istio-injection=enabled --overwrite >/dev/null
      kubectl delete peerauthentication --all -n "$NAMESPACE" --ignore-not-found >/dev/null 2>&1 || true
      kubectl apply -f "$REPO_ROOT/deploy/k8s/mesh/peer-authentication-disable.yaml" >/dev/null
      export GHZ_SIDECAR_ANNOTATION='sidecar.istio.io/inject: "true"'
      export K6_SIDECAR_ANNOTATION='sidecar.istio.io/inject: "true"'
      ;;
    C)
      log "Cenario C: mesh com mTLS STRICT"
      kubectl label namespace "$NAMESPACE" istio-injection=enabled --overwrite >/dev/null
      kubectl delete peerauthentication --all -n "$NAMESPACE" --ignore-not-found >/dev/null 2>&1 || true
      kubectl apply -f "$REPO_ROOT/deploy/k8s/mesh/peer-authentication-strict.yaml" >/dev/null
      kubectl apply -f "$REPO_ROOT/deploy/k8s/mesh/peer-authentication-battle-ws.yaml" >/dev/null
      export GHZ_SIDECAR_ANNOTATION='sidecar.istio.io/inject: "true"'
      export K6_SIDECAR_ANNOTATION='sidecar.istio.io/inject: "true"'
      ;;
    *) echo "Cenario desconhecido: $scenario" >&2; exit 1 ;;
  esac

  log "Recriando pods dos apps (rollout restart) para aplicar a config do cenario..."
  # shellcheck disable=SC2086
  kubectl rollout restart deployment $APP_DEPLOYMENTS -n "$NAMESPACE" >/dev/null
  # shellcheck disable=SC2086
  kubectl rollout status deployment $APP_DEPLOYMENTS -n "$NAMESPACE" --timeout="${JOB_TIMEOUT}s"
  log "Aguardando ${SETTLE_SECONDS}s para estabilizar..."
  sleep "$SETTLE_SECONDS"
}

run_job() {
  # $1 = ghz|k6 ; $2 = arquivo de saida
  local kind="$1" outfile="$2"
  local tpl="$SCRIPT_DIR/k8s/${kind}-job.yaml.tpl"
  local job="bench-${kind}"

  kubectl delete job "$job" -n "$NAMESPACE" --ignore-not-found >/dev/null 2>&1 || true
  envsubst < "$tpl" | kubectl apply -f - >/dev/null

  # Espera completar OU falhar
  if ! kubectl wait --for=condition=complete "job/$job" -n "$NAMESPACE" --timeout="${JOB_TIMEOUT}s" 2>/dev/null; then
    kubectl wait --for=condition=failed "job/$job" -n "$NAMESPACE" --timeout=10s 2>/dev/null || true
    log "AVISO: job $job nao completou no tempo; coletando logs mesmo assim."
  fi

  # Coleta o stdout do container de carga (evita logs do sidecar com -c)
  kubectl logs "job/$job" -n "$NAMESPACE" -c "$kind" > "$outfile" 2>/dev/null || \
    kubectl logs "job/$job" -n "$NAMESPACE" > "$outfile" 2>/dev/null || true
  kubectl delete job "$job" -n "$NAMESPACE" --ignore-not-found >/dev/null 2>&1 || true
}

collect_resources() {
  local outfile="$1"
  kubectl top pods -n "$NAMESPACE" --containers 2>/dev/null > "$outfile" || \
    echo "metrics-server indisponivel (kubectl top falhou)" > "$outfile"
}

# ----- loop principal -----
for scenario in $SCENARIOS; do
  configure_mesh "$scenario"

  # Warm-up (descartado): 1 rodada curta para aquecer conexoes/JIT
  log "[$scenario] warm-up..."
  if [ "$RUN_GHZ" = "1" ]; then GHZ_N=2000 GHZ_SKIP=0 run_job ghz "$OUT/_warmup_ghz_$scenario.json" || true; fi

  for rep in $(seq 1 "$REPS"); do
    repdir="$OUT/$scenario/rep$rep"
    mkdir -p "$repdir"
    log "[$scenario] repeticao $rep/$REPS"

    if [ "$RUN_GHZ" = "1" ]; then
      run_job ghz "$repdir/ghz.json"
    fi
    if [ "$RUN_K6" = "1" ]; then
      # coleta recursos durante a janela do k6 (que dura K6_DURATION)
      ( sleep "$SETTLE_SECONDS"; collect_resources "$repdir/top.txt" ) &
      run_job k6 "$repdir/k6.log"
      wait || true
    else
      collect_resources "$repdir/top.txt"
    fi
  done
done

log "Coleta concluida. Agregando resultados..."
if command -v python3 >/dev/null 2>&1; then
  python3 "$SCRIPT_DIR/aggregate.py" "$OUT" || log "AVISO: agregacao falhou; dados brutos em $OUT"
else
  log "python3 indisponivel; pule para a agregacao manual (dados brutos em $OUT)."
fi

log "Pronto. Resultados em: $OUT"
