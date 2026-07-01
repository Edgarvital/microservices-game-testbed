# Resultados do benchmark

Gerado por `bench/aggregate.py`. Valores: media ± desvio padrao entre repeticoes.

## gRPC: battle-service -> inventory-service (GetBattleStats)

| Metrica | A - sem mesh | B - mesh, mTLS OFF | C - mesh, mTLS ON |
|---|---|---|---|
| Throughput (req/s) | 3601.83 ± 343.60 | 2589.42 ± 114.89 | 2470.50 ± 88.43 |
| Latencia media (ms) | 10.87 ± 1.46 | 16.40 ± 0.76 | 17.26 ± 0.64 |
| p50 (ms) | 10.40 ± 1.28 | 16.21 ± 0.89 | 16.92 ± 0.59 |
| p95 (ms) | 18.20 ± 2.75 | 24.74 ± 0.97 | 26.17 ± 1.05 |
| p99 (ms) | 22.96 ± 4.16 | 28.78 ± 1.44 | 30.63 ± 1.64 |

### Overhead relativo (latencia media)
- A -> C: +58.7%
- B -> C: +5.2%

## REST: cliente -> matchmaking-service (/join)

| Metrica | A - sem mesh | B - mesh, mTLS OFF | C - mesh, mTLS ON |
|---|---|---|---|
| Throughput (req/s) | 5406.98 ± 39.35 | 2556.83 ± 52.02 | 2428.52 ± 30.18 |
| Latencia media (ms) | 5.44 ± 0.04 | 11.64 ± 0.24 | 12.25 ± 0.15 |
| p50 (ms) | 5.24 ± 0.03 | 11.39 ± 0.20 | 12.01 ± 0.13 |
| p95 (ms) | 7.80 ± 0.10 | 15.32 ± 0.35 | 16.18 ± 0.33 |
| p99 (ms) | 9.63 ± 0.23 | 17.85 ± 0.40 | 18.77 ± 0.45 |

### Overhead relativo (latencia media)
- A -> C: +125.4%
- B -> C: +5.3%
