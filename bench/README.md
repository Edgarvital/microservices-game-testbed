# Benchmark de overhead do mTLS (feature flag)

Harness reproduzível para medir o impacto de desempenho da criptografia east-west (mTLS via Istio) sobre os serviços do testbed. É a base experimental do artigo focado em desempenho.

**Princípio:** o harness não inventa dados. Ele executa carga real contra o cluster e resume o que os geradores de carga mediram. Os números só têm valor se produzidos num ambiente controlado (ver "Validade científica").

## Desenho experimental

Três cenários, isolando o custo do sidecar do custo da criptografia:

| Cenário | Sidecar | PeerAuthentication | Mede |
|---------|:-------:|:------------------:|------|
| **A** | não | (n/a) | linha de base pura, sem mesh |
| **B** | sim | `DISABLE` | overhead só do sidecar |
| **C** | sim | `STRICT` (+ exceção do WS do battle) | sidecar + criptografia |

- **B → C** isola o overhead **da criptografia** (comparação principal para a RQ1).
- **A → C** mede o custo total do hardening de comunicação.

Alvos de carga:
- **gRPC (principal):** `battle-service → inventory-service` (`GetBattleStats`). Salto east-west isolado, HTTP/2, sem autenticação. Ferramenta: [ghz](https://ghz.sh/).
- **REST (secundário):** `cliente → matchmaking-service /join`, que internamente chama o inventory. Fluxo end-to-end. Ferramenta: [k6](https://k6.io/).

A carga roda **de dentro do mesh** (Jobs com sidecar), senão o modo `STRICT` rejeitaria conexões externas sem mTLS. Isso segue as [best practices de benchmark do Istio](https://istio.io/latest/blog/2019/performance-best-practices/): baseline sem mesh, mTLS STRICT explícito, warm-up antes da medição.

## Pré-requisitos

1. **Cluster Kubernetes** com os serviços implantados (`kubectl apply -k deploy/k8s/<serviço>` para os quatro serviços).
2. **Istio** instalado (`istioctl install`). Recomendado habilitar **native sidecars** (`values.pilot.env.ENABLE_NATIVE_SIDECARS=true`) ou usar **Istio Ambient** — sem isso, Jobs com sidecar clássico podem não completar (o Envoy não encerra sozinho); ver "Armadilhas".
3. **metrics-server** (para `kubectl top`).
4. **Inventário de teste semeado** para o gRPC não cair em `NOT_FOUND`:
   ```bash
   kubectl exec -i deploy/inventory-db -n onlinegame -- \
     psql -U postgres -d onlinegame_inventory < bench/seed/seed-inventory.sql
   ```
5. Ferramentas locais: `kubectl`, `envsubst`, `python3` (para a agregação).

## Execução

```bash
# padrao: cenarios A B C, 5 repeticoes cada
bash bench/run.sh

# exemplos de override
REPS=10 GHZ_N=50000 GHZ_C=100 bash bench/run.sh
SCENARIOS="B C" RUN_K6=0 bash bench/run.sh     # so gRPC, so mesh
```

Parâmetros (variáveis de ambiente, com defaults em `run.sh`): `NAMESPACE`, `REPS`, `SCENARIOS`, `PLAYER_ID`, `GHZ_N/GHZ_C/GHZ_CONN/GHZ_SKIP`, `K6_VUS/K6_DURATION`, `GHZ_IMAGE/K6_IMAGE`, `RUN_GHZ/RUN_K6`, `SETTLE_SECONDS`, `JOB_TIMEOUT`.

O que o `run.sh` faz por cenário: ajusta a injeção de sidecar e o `PeerAuthentication`, faz `rollout restart` + espera estabilizar, roda um warm-up descartado, e então N repetições coletando `ghz.json`, `k6.log` e `kubectl top`. Ao final chama `aggregate.py`.

## Saída

```
bench/results/<stamp>/
├── environment.txt          # metadados (versões, nós, parâmetros) - para replicar
├── A/rep1/{ghz.json,k6.log,top.txt} ...
├── B/... C/...
├── summary.csv              # media/desvio por cenario/metrica
└── summary.md               # tabelas prontas para o artigo
```

Copie `summary.md` para o `RESULTS_TEMPLATE.md` (ou cite direto). Os diretórios brutos ficam versionados para auditoria (o `.gitignore` local mantém só o template versionado por padrão; comite os `results/<stamp>/` que quiser preservar com `git add -f`).

## Validade científica (ler antes de gerar dados para o artigo)

Para os números serem defensáveis na banca:

- **Ambiente controlado e dedicado.** Não use laptop/Docker Desktop/kind para os números finais: o ruído invalida a medição. Use um cluster com nós dedicados e sem carga concorrente.
- **Recursos fixos.** Defina `requests`/`limits` iguais em todos os cenários e réplicas fixas. Varie **apenas** a flag.
- **Repetições + dispersão.** Rode `REPS>=5` (idealmente 10+) e reporte média ± desvio (ou IC 95%). O `aggregate.py` já calcula média e desvio.
- **Warm-up.** Já embutido (ghz `--skipFirst` e uma rodada de descarte por cenário).
- **Separe gRPC e REST.** O overhead relativo do mTLS difere entre HTTP/2 (multiplexado, conexão persistente) e HTTP/1.1.
- **Recursos.** Reporte CPU/memória do sidecar (`top.txt`): o custo do mTLS é latência **e** CPU.
- **Documente tudo** (`environment.txt` faz isso automaticamente): versão do Istio, modo (sidecar/ambient), specs dos nós, parâmetros de carga.

## Armadilhas conhecidas

- **Jobs que não completam com sidecar clássico:** o Envoy não encerra junto do container de carga. Solução: habilitar native sidecars no Istio, ou usar Ambient. O `run.sh` tem timeout e coleta os logs mesmo se o Job não marcar "complete".
- **`STRICT` + carga externa:** carga via `port-forward` do host é rejeitada no modo STRICT (sem mTLS). Por isso a carga roda como Job **dentro** do mesh.
- **gRPC sem reflection:** o inventory não expõe reflection, então o ghz usa o `proto/player_inventory.proto` (montado via ConfigMap pelo `run.sh`).
- **`NOT_FOUND` no gRPC:** sem o inventário semeado, `GetBattleStats` retorna erro (caminho mais curto) e distorce a medição. Rode o seed. Mesmo sem seed, a comparação relativa A/B/C continua válida se o caminho for consistente entre cenários.
- **Redis/Postgres fora do mesh:** o tráfego para a infra não é coberto pelo mTLS (ver `deploy/k8s/mesh/README.md`). O fluxo matchmaking→battle passa por Redis e não entra na medição do mTLS.

## Referências

- [Istio - Best Practices: Benchmarking Service Mesh Performance](https://istio.io/latest/blog/2019/performance-best-practices/)
- [ghz - gRPC benchmarking and load testing tool](https://ghz.sh/)
- [k6 - load testing](https://k6.io/)
- [Technical Report: Performance Comparison of Service Mesh Frameworks (mTLS)](https://arxiv.org/abs/2411.02267)
