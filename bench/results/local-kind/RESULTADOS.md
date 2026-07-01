# Resultados do benchmark de overhead do mTLS — rodada local (kind)

Primeira rodada de medição, em ambiente **local (kind)**, do overhead de desempenho da criptografia east-west (mTLS via Istio). Serve como cenário preliminar; uma rodada em nuvem (multi-nó) complementará estes dados.

> Os números foram **medidos**, não estimados. Dados brutos por repetição em `A/`, `B/`, `C/`; agregação em `summary.md`/`summary.csv`.

## Ambiente

| Item | Valor |
|------|-------|
| Cluster | kind (single-node), Kubernetes **v1.36.1** |
| Host | Windows 11 + Docker Desktop (WSL2), nó Debian 13 |
| Service mesh | **Istio 1.30.2**, perfil minimal, native sidecars |
| mTLS | `PeerAuthentication` STRICT (cenário C) |
| Réplicas | 1 por serviço |
| Carga gRPC (ghz) | n=10000 (skipFirst=1000 → 9000 medidos), c=50, conn=10 |
| Carga REST (k6) | 30 VUs, 20 s |
| Repetições | 8 por cenário (todas válidas) |

Cenários: **A** = sem mesh · **B** = mesh, mTLS OFF (`DISABLE`) · **C** = mesh, mTLS ON (`STRICT`).

## gRPC: battle-service → inventory-service (`GetBattleStats`)

Salto east-west isolado (HTTP/2, sem autenticação). É a métrica principal.

| Métrica | A - sem mesh | B - mesh, mTLS OFF | C - mesh, mTLS ON |
|---|---|---|---|
| Throughput (req/s) | 3601.8 ± 343.6 | 2589.4 ± 114.9 | 2470.5 ± 88.4 |
| Latência média (ms) | 10.87 ± 1.46 | 16.40 ± 0.76 | 17.26 ± 0.64 |
| p50 (ms) | 10.40 | 16.21 | 16.92 |
| p95 (ms) | 18.20 | 24.74 | 26.17 |
| p99 (ms) | 22.96 | 28.78 | 30.63 |

- **Overhead de latência média — B → C (custo isolado do mTLS): +5,2%**
- Overhead de latência média — A → C (sidecar + mTLS): +58,7%
- Throughput: A→B −28%, **B→C −4,6%**

## REST: cliente → matchmaking-service (`/join`)

Fluxo end-to-end (k6 → matchmaking → inventory + Redis/DB).

| Métrica | A - sem mesh | B - mesh, mTLS OFF | C - mesh, mTLS ON |
|---|---|---|---|
| Throughput (req/s) | 5407.0 ± 39.4 | 2556.8 ± 52.0 | 2428.5 ± 30.2 |
| Latência média (ms) | 5.44 ± 0.04 | 11.64 ± 0.24 | 12.25 ± 0.15 |
| p50 (ms) | 5.24 | 11.39 | 12.01 |
| p95 (ms) | 7.80 | 15.32 | 16.18 |
| p99 (ms) | 9.63 | 17.85 | 18.77 |

- **Overhead de latência média — B → C (custo isolado do mTLS): +5,3%**
- Overhead de latência média — A → C (sidecar + mTLS): +125,4%
- Throughput: A→B −53%, **B→C −5,0%**

## Uso de recursos sob carga (cenário C, carga gRPC no inventory)

`kubectl top pods --containers` durante carga sustentada (`C_resources_under_load.txt`):

| Container | CPU | Memória |
|-----------|-----|---------|
| inventory-service (app) | ~880 m | ~472 Mi |
| inventory-service **istio-proxy (sidecar)** | ~380 m | ~40 Mi |

O sidecar Envoy adiciona ~40% de CPU relativo ao app sob essa carga, mais ~40 Mi de memória fixa por pod. O custo do mTLS é **latência e CPU**.

## Achado principal

Consistente entre gRPC e REST: **a maior parte da degradação vem do sidecar (A→B), não da criptografia em si.** Habilitar mTLS sobre o mesh já presente (B→C) custa apenas **~5% de latência e ~5% de throughput**. Ou seja, uma vez pago o preço de entrar no mesh, ligar a criptografia é barato — argumento a favor de adotar mTLS quando o mesh já é usado.

## Limitações desta rodada (ambiente local)

Registrar no artigo:

1. **Single-node (kind):** todo o tráfego pod-a-pod é loopback no mesmo nó, sem latência de rede física. Como a latência base é mínima, o overhead **relativo** do sidecar/mTLS tende a ser **maior** aqui do que em cluster multi-nó. Em nuvem, espere overhead relativo menor (a latência de rede dilui o custo fixo do Envoy).
2. **Host compartilhado (Windows/Docker Desktop/WSL2):** jitter do ambiente; o cenário A (sem sidecar, muito rápido) é o mais sensível — daí o desvio maior no throughput gRPC de A.
3. **Carga modesta:** c=50–80, 8 repetições. Suficiente para a tendência; para o artigo final, aumentar carga e repetições.
4. **REST parcial:** o endpoint REST do inventory exige JWT, então a chamada interna matchmaking→inventory retorna 401 rápido (fallback do `MockPowerProvider`). O k6 ainda mede o overhead do sidecar no matchmaking, mas o salto ao inventory é curto.
5. **Versões:** K8s 1.36.1, Istio 1.30.2 (sidecar clássico com native sidecars), ghz/k6 `latest`. Fixar versões na rodada final.

## Reproduzir

```bash
# por cenario (foreground), pasta compartilhada:
OUT="$PWD/bench/results/<nome>" AGGREGATE=0 SCENARIOS="A" REPS=8 GHZ_N=10000 K6_DURATION=20s bash bench/run.sh
# ... B ... e para C use AGGREGATE=1
python bench/aggregate.py bench/results/<nome>
```
</content>
