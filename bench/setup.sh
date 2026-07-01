#!/usr/bin/env bash
#
# Bootstrap do ambiente de benchmark num cluster ja acessivel (kubeconfig ativo):
# instala Istio, implanta os 4 servicos, semeia o inventario de teste e valida.
# Idempotente onde possivel. Nao provisiona o cluster (ver bench/RUNBOOK.md).
#
# Uso:
#   bash bench/setup.sh                          # imagens onlinegame/*:baseline
#   REGISTRY=docker.io/edgarvital bash bench/setup.sh   # override p/ registry remoto
#
set -euo pipefail

NAMESPACE=${NAMESPACE:-onlinegame}
REGISTRY=${REGISTRY:-}                 # vazio = usa as imagens dos manifests (onlinegame/*)
TAG=${TAG:-baseline}
ISTIO_PROFILE=${ISTIO_PROFILE:-demo}
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

require() { command -v "$1" >/dev/null 2>&1 || { echo "ERRO: '$1' nao encontrado." >&2; exit 1; }; }
require kubectl
require istioctl
kubectl cluster-info >/dev/null 2>&1 || { echo "ERRO: nenhum cluster acessivel (kubeconfig)." >&2; exit 1; }
echo "Contexto: $(kubectl config current-context)"

echo "==> Instalando Istio (profile=$ISTIO_PROFILE, native sidecars)"
istioctl install -y --set "profile=$ISTIO_PROFILE" \
  --set values.pilot.env.ENABLE_NATIVE_SIDECARS=true

apply_service() {
  local svc="$1"
  local base="$REPO_ROOT/deploy/k8s/$svc"
  if [ -z "$REGISTRY" ]; then
    kubectl apply -k "$base"
    return
  fi
  # Override de imagem para o registry remoto, sem editar os manifests do repo.
  local tmp; tmp="$(mktemp -d)"
  cat > "$tmp/kustomization.yaml" <<EOF
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
namespace: $NAMESPACE
resources:
  - $base
images:
  - name: onlinegame/$svc
    newName: $REGISTRY/onlinegame/$svc
    newTag: $TAG
EOF
  kubectl apply -k "$tmp"
  rm -rf "$tmp"
}

echo "==> Implantando servicos no namespace $NAMESPACE"
for svc in auth-service inventory-service matchmaking-service battle-service; do
  apply_service "$svc"
done

echo "==> Aguardando rollout"
kubectl rollout status deployment -n "$NAMESPACE" --timeout=300s

echo "==> Semeando inventario de teste"
kubectl exec -i deploy/inventory-db -n "$NAMESPACE" -- \
  psql -U postgres -d onlinegame_inventory < "$REPO_ROOT/bench/seed/seed-inventory.sql"

echo "==> Estado final"
kubectl get pods -n "$NAMESPACE" -o wide

cat <<EOF

Setup concluido. Proximos passos:
  1) Confira que os pods estao Running.
  2) Rode o benchmark:  bash bench/run.sh
  3) Resultados agregados em bench/results/<stamp>/summary.md
EOF
