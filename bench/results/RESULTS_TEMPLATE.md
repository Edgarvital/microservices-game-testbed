# Resultados do benchmark de overhead do mTLS (template)

> Preencha com a saída de `bench/aggregate.py` após cada execução. Não edite números à mão: cole o que o harness mediu. Guarde também os diretórios brutos `results/<stamp>/` para auditoria/replicabilidade.

## Ambiente de execução

| Item | Valor |
|------|-------|
| Data/hora (UTC) | |
| Cluster (provedor / versão k8s) | |
| Nós (qtd / CPU / RAM / tipo) | |
| Istio (versão / modo: sidecar ou ambient) | |
| Réplicas por serviço | |
| Requests/limits dos pods | |
| Ferramenta de carga (ghz / k6 versões) | |
| Parâmetros ghz (n / c / conn / skipFirst) | |
| Parâmetros k6 (VUs / duração) | |
| Repetições por cenário | |

## gRPC: battle-service → inventory-service (GetBattleStats)

_Métrica principal (salto east-west isolado, HTTP/2, sem autenticação)._

| Métrica | A - sem mesh | B - mesh, mTLS OFF | C - mesh, mTLS ON |
|---|---|---|---|
| Throughput (req/s) | | | |
| Latência média (ms) | | | |
| p50 (ms) | | | |
| p95 (ms) | | | |
| p99 (ms) | | | |

Overhead relativo (latência média): A→C ____%, B→C ____% (custo isolado da criptografia).

## REST: cliente → matchmaking-service (/join)

_Métrica secundária (fluxo end-to-end: k6→matchmaking→inventory + Redis/DB)._

| Métrica | A - sem mesh | B - mesh, mTLS OFF | C - mesh, mTLS ON |
|---|---|---|---|
| Throughput (req/s) | | | |
| Latência média (ms) | | | |
| p50 (ms) | | | |
| p95 (ms) | | | |
| p99 (ms) | | | |

## Uso de recursos (Envoy sidecar)

| Cenário | CPU app (m) | CPU sidecar (m) | Mem app (Mi) | Mem sidecar (Mi) |
|---|---|---|---|---|
| A | | (n/a) | | (n/a) |
| B | | | | |
| C | | | | |

## Observações

- Distribuição das latências (bimodal?), erros, saturação de CPU, etc.
- Limitações do experimento (ver `bench/README.md`).
