# Roadmap de segurança

Este documento conecta o testbed à pesquisa de mestrado ([RSL](./Mestrado/RSL%20Parcial/main.md)). O código está na **linha de base "Unprotected" (Fase 1)**: o objetivo é usá-la como o "antes" e aplicar, sobre ela, as contramedidas criptográficas que a Revisão Sistemática da Literatura apontou como mais efetivas, medindo o "depois".

## Pergunta de pesquisa (RQ1)

> Como a aplicação de um conjunto de contramedidas baseadas em técnicas criptográficas e padrões de arquitetura segura pode mitigar de forma efetiva as vulnerabilidades de segurança mais recorrentes em uma arquitetura de microsserviços orquestrada por Kubernetes?

A RSL destacou três pilares de defesa em profundidade: **mTLS** (tráfego east-west), **gestão dinâmica de segredos / rotação** (ex.: Vault) e **network policies** (modelo Zero Trust).

## Estado atual (baseline Unprotected)

Mapeando o código contra as vulnerabilidades recorrentes identificadas na RSL:

| Vulnerabilidade (RSL) | Presente no testbed? | Onde |
|-----------------------|:--------------------:|------|
| Configurações padrão inseguras | Sim | `ASPNETCORE_ENVIRONMENT=Development` em produção, Swagger exposto como probe, réplica única |
| Comunicação east-west sem criptografia | Sim | matchmaking -> inventory (REST) e battle -> inventory (gRPC `insecure`) em texto claro; sem TLS entre pods |
| Gestão inadequada de segredos | Sim | `JWT_SECRET` hardcoded no Compose e em ConfigMap (inventory/matchmaking); segredo HS256 compartilhado |
| Ausência de autenticação entre serviços | Sim | matchmaking e battle sem autenticação; gRPC `GetBattleStats` do inventory é anônimo; chamadas sem credencial |
| Falta de isolamento de rede | Sim | namespace único, sem `NetworkPolicy` |

Pontos adicionais observados:
- Comparação de `passwordHash` no auth não é constant-time.
- JWT sem validação de issuer/audience no inventory (REST).
- Sem rate limiting nos endpoints de login.
- battle-service: WebSocket sem TLS/WSS, `CheckOrigin` aceita qualquer origem, gRPC `insecure`.
- Canal Redis Pub/Sub (`battle:match-found`) sem autenticação nem criptografia.

## Ativos já presentes para a Fase 2

O código já tem "ganchos" que facilitam aplicar as contramedidas:

- **`ISecretProvider` com `VaultSecretProvider`** no auth-service: integração KV v2 pronta para HashiCorp Vault. Basta `Secrets:Provider=Vault` + configurar `Vault:*`. Base direta para a contramedida de **gestão dinâmica de segredos**.
- **Kustomize por serviço**: permite adicionar overlays de hardening sem tocar na base.
- **Separação clara de config** (ConfigMap) vs segredo (Secret) já iniciada no auth.

## Contramedidas a aplicar (Fase 2)

### 1. mTLS (east-west) — implementado como feature flag
- **Já disponível** via Istio em `deploy/k8s/mesh/` (ver [README do mesh](../deploy/k8s/mesh/README.md)).
- A criptografia é uma **feature flag** (`PeerAuthentication` `STRICT` vs `DISABLE`) para ligar/desligar sem alterar os serviços, permitindo medir o overhead.
- Alvos atuais: **battle -> inventory (gRPC/HTTP2)** e matchmaking -> inventory (REST/HTTP1). Estabelece identidade criptográfica por serviço (mitiga spoofing e MitM).
- Cobertura, exceção do WebSocket externo e limites (Redis como intermediário) analisados no [README do mesh](../deploy/k8s/mesh/README.md).
- Desenho experimental (baseline pura / mesh sem cripto / mesh com mTLS) documentado no README do mesh, isolando o custo da criptografia do custo do sidecar.

### 2. Gestão dinâmica de segredos
- Migrar `JWT_SECRET` e connection strings de ConfigMap/Compose para o Vault.
- Ativar `Secrets:Provider=Vault` no auth; estender o mesmo padrão a inventory e matchmaking.
- Rotação automática do segredo HS256 (ou migração para chaves assimétricas RS256, eliminando o segredo compartilhado).

### 3. Network policies (Zero Trust)
- `NetworkPolicy` restringindo tráfego: só matchmaking fala com inventory; só cada serviço fala com seu próprio banco; negar todo o resto por padrão.
- Segmentação lógica dentro do namespace `onlinegame`.

### 4. Endurecimento complementar
- Autenticação serviço-a-serviço (matchmaking apresentando credencial ao inventory).
- Validação de issuer/audience no JWT; comparação de senha constant-time; rate limiting.
- `ASPNETCORE_ENVIRONMENT=Production`, remover Swagger dos probes, TLS na borda.

## Validação (métricas da RSL)

A RSL aponta duas abordagens complementares para avaliar efetividade, aplicáveis aqui:

- **Redução de vulnerabilidades exploráveis:** rodar **Kube-bench** (conformidade CIS) e **Kube-hunter** (pentest) antes e depois, comparando o score do cluster.
- **Impacto de desempenho (overhead):** medir latência e throughput antes e depois do mTLS, quantificando o custo da criptografia. Meça REST e gRPC separadamente (o gRPC battle -> inventory usa HTTP/2 multiplexado; o overhead relativo do mTLS costuma diferir do REST HTTP/1.1).

Esse par (segurança medida x overhead medido) é o resultado central que o testbed pretende produzir para responder à RQ1.
</content>
