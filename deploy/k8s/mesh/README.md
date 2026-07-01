# Flag de criptografia entre serviços (mTLS via Istio)

Esta pasta implementa a **feature flag** que liga/desliga a criptografia do tráfego *east-west* (serviço-a-serviço) para os experimentos de overhead do mestrado.

A flag é o campo `spec.mtls.mode` de um objeto `PeerAuthentication` do Istio, alternado entre `STRICT` (criptografia ON) e `DISABLE` (OFF). **Os serviços e suas imagens não mudam entre os cenários** - a criptografia é aplicada no data plane (sidecar Envoy), o que isola a criptografia como única variável do experimento.

## Arquivos

| Arquivo | Papel |
|---------|-------|
| `namespace-istio-injection.yaml` | Habilita injeção de sidecar no namespace `onlinegame` |
| `peer-authentication-strict.yaml` | Flag = **ON** (mTLS obrigatório) |
| `peer-authentication-disable.yaml` | Flag = **OFF** (texto claro) |
| `peer-authentication-battle-ws.yaml` | Exceção: mantém a porta WebSocket do battle-service em PERMISSIVE (ver "Cobertura") |

## Cobertura da flag (o que ela criptografa)

A flag atua sobre o tráfego **east-west entre pods que têm sidecar**. Mapeamento contra as comunicações reais do sistema:

| Comunicação | Protocolo | Coberta pela flag? | Observação |
|-------------|-----------|:------------------:|------------|
| battle-service -> inventory-service | gRPC (HTTP/2) | **Sim** | Principal alvo. Ambos no mesh; mTLS transparente. |
| matchmaking -> inventory-service | REST (HTTP/1.1) | **Sim** | Consulta de poder do jogador. |
| matchmaking -> battle-service | via Redis Pub/Sub | **Não (indireto)** | Passa pelo Redis; só seria mTLS se o Redis entrasse no mesh (ver abaixo). |
| serviços -> Redis / PostgreSQL | TCP | **Não por padrão** | Infra sem sidecar. O tráfego para fora do mesh sai em texto claro. |
| cliente -> battle-service | WebSocket | **Não (north-south)** | Tráfego externo; ver a exceção obrigatória abaixo. |

### Ponto de atenção 1 - WebSocket externo quebra sob STRICT
Com `default=STRICT`, o sidecar do battle exige mTLS de toda conexão de entrada, inclusive o WebSocket de clientes fora do mesh (que não têm certificado). Aplique `peer-authentication-battle-ws.yaml` junto com o STRICT para deixar a porta 8080 do battle em PERMISSIVE. Alternativa mais fiel ao Zero Trust: expor o battle via **Istio Ingress Gateway** (termina o tráfego externo) em vez da exceção.

### Ponto de atenção 2 - gRPC e REST na mesma porta do inventory
O inventory serve REST (HTTP/1.1) e gRPC (HTTP/2 cleartext) na **mesma porta 8080** (Kestrel `Http1AndHttp2`). O mTLS opera na camada de transporte e funciona nos dois casos, mas a detecção de protocolo L7 do Istio fica ambígua (afeta telemetria/roteamento fino, não a criptografia). Para telemetria gRPC limpa, considere expor o gRPC numa porta dedicada no Kestrel e nomeá-la `grpc` no Service.

### Ponto de atenção 3 - Redis como intermediário
O fluxo matchmaking -> battle é desacoplado via Redis Pub/Sub. Para criptografá-lo de ponta a ponta seria preciso colocar o Redis no mesh (sidecar + `STRICT`) ou habilitar TLS nativo do Redis. Está fora do escopo da flag atual; documente isso ao reportar os resultados para não superestimar a cobertura.

## Pré-requisito: instalar o Istio

```bash
istioctl install --set profile=demo -y
```

## Preparação (uma vez)

1. Aplicar os serviços normalmente (ver `../../README.md`).
2. Habilitar a injeção de sidecar e recriar os pods:

```bash
kubectl apply -f deploy/k8s/mesh/namespace-istio-injection.yaml
kubectl rollout restart deployment -n onlinegame
kubectl rollout status deployment -n onlinegame
```

Confirme que cada pod agora tem 2 containers (app + `istio-proxy`):

```bash
kubectl get pods -n onlinegame
```

## Alternar a flag

**Criptografia ON:**
```bash
kubectl apply -f deploy/k8s/mesh/peer-authentication-strict.yaml
# necessário para não quebrar o WebSocket externo do battle-service:
kubectl apply -f deploy/k8s/mesh/peer-authentication-battle-ws.yaml
```

**Criptografia OFF:**
```bash
kubectl apply -f deploy/k8s/mesh/peer-authentication-disable.yaml
```

A troca é quase instantânea (não recria pods). Verifique o modo ativo:
```bash
kubectl get peerauthentication -n onlinegame -o yaml
```

## Cenários de medição sugeridos

Para separar o custo do sidecar do custo da criptografia, meça três configurações:

| Cenário | Injeção de sidecar | PeerAuthentication | Mede |
|---------|:------------------:|:------------------:|------|
| A - Baseline pura | desabilitada | (n/a) | linha de base sem mesh |
| B - Mesh sem cripto | habilitada | `DISABLE` | overhead só do sidecar |
| C - Mesh com mTLS | habilitada | `STRICT` (+ exceção do battle) | sidecar + criptografia |

- **B → C** isola o overhead **da criptografia** (a comparação principal para a RQ1).
- **A → C** mede o custo total do hardening de comunicação.

Para o cenário A, aplique um namespace sem a label (`../auth-service/namespace.yaml` etc.) e faça `rollout restart`.

## Como medir

O caminho east-west mais relevante hoje é o **gRPC battle-service → inventory-service** (`GetBattleStats`), acionado quando uma partida é criada. Um caminho secundário é **matchmaking → inventory** (REST, dentro do `join`). Abordagens:

- **gRPC (battle → inventory):** rode `ghz` (gerador de carga gRPC) contra o inventory, ou dispare partidas publicando `MatchFoundEvent` no canal Redis `battle:match-found` e meça o tempo de criação de sala. É o alvo principal, pois é o novo fluxo e usa HTTP/2.
- **REST (matchmaking → inventory):** carga no endpoint `join` (k6, hey, wrk).
- **Fortio/ghz** para medir diretamente a chamada serviço-a-serviço com latência controlada.

Registre em cada cenário: latência (p50/p95/p99), throughput (req/s) e uso de CPU/memória dos pods (`kubectl top pods -n onlinegame`), já que o Envoy consome recursos. Como o gRPC é HTTP/2 (conexão multiplexada e persistente) e o REST é HTTP/1.1, meça-os separadamente: o overhead relativo do mTLS costuma diferir entre os dois.

> Dica de rigor: fixe réplicas, recursos (requests/limits) e a carga entre os cenários; varie **apenas** a flag. Rode várias repetições e reporte média + desvio.

## Escopo e limites

- Cobre só a criptografia de comunicação (pilar mTLS da RSL). Os outros pilares (gestão de segredos com Vault, network policies) são independentes e não devem ser alterados durante estas medições, para não confundir variáveis.
- Vale como alternativa ao Istio o **Linkerd** (mais leve, overhead menor), caso queira um segundo ponto de comparação de mesh. A flag equivalente é a anotação/`Server`+`ServerAuthorization`; a metodologia é a mesma.
</content>
