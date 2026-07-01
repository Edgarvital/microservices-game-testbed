# Infraestrutura e deploy

Dois alvos de deploy: **Docker Compose** (desenvolvimento local) e **Kubernetes** (namespace `onlinegame`, Kustomize por serviço). Ambos refletem a linha de base "Unprotected": segredos e conexões em texto claro por design.

## Docker Compose

Arquivo: `deploy/docker-compose.yml`.

```bash
cd deploy
docker compose up -d --build
docker compose down
```

### Serviços e portas

| Container | Imagem | Porta host | Porta interna |
|-----------|--------|-----------|---------------|
| auth-service | build local | 8080 | 8080 |
| inventory-service | build local | 8081 | 8080 |
| matchmaking-service | build local | 8082 | 8080 |
| battle-service | build local | 8083 | 8080 |
| auth-db | postgres:16-alpine | 5432 | 5432 |
| inventory-db | postgres:16-alpine | 5433 | 5432 |
| matchmaking-db | postgres:16-alpine | 5434 | 5432 |
| redis | redis:7-alpine | 6379 | 6379 |

Swagger: `http://localhost:8080/swagger`, `:8081/swagger`, `:8082/swagger`. O battle-service não tem Swagger: expõe `ws://localhost:8083/ws` e `http://localhost:8083/healthz`.

O build do inventory-service usa o **repositório inteiro como contexto** (não a pasta do serviço), porque o Dockerfile precisa copiar também `proto/player_inventory.proto` para gerar o stub gRPC.

### Dependências e ordem de subida
- `auth-service` depende de `auth-db` (healthy) e `redis` (healthy).
- `inventory-service` depende de `inventory-db` (healthy).
- `matchmaking-service` depende de `matchmaking-db` (healthy) e `redis` (healthy).
- `battle-service` depende de `redis` (healthy); usa o `inventory-service` via gRPC em runtime.

Os Postgres têm healthcheck `pg_isready` e o Redis `redis-cli ping`. Dados persistem em `deploy/docker-data/` (ignorado pelo git).

### Variáveis de ambiente relevantes (Compose)
- Todos: `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://+:8080`.
- auth: `AUTH_DB_CONNECTION`, `JWT_SECRET=CHANGE_ME_BASELINE_LOCAL_SECRET_32B`, `Secrets__Provider=Local`, `Jwt__AccessTokenTtlMinutes=120`.
- inventory: `ConnectionStrings__InventoryDb`, `Jwt__Secret=CHANGE_ME_BASELINE_LOCAL_SECRET_32B` (mesmo segredo do auth, para validar o token).
- matchmaking: `ConnectionStrings__MatchmakingDb`, `ConnectionStrings__Redis=redis:6379`.
- battle: `HTTP_ADDR=:8080`, `REDIS_ADDR=redis:6379`, `INVENTORY_GRPC_ADDR=inventory-service:8080`, `MATCH_CHANNEL=battle:match-found`.

> **Config de integração do matchmaking:** o Compose define `Services__InventoryServiceUrl=http://inventory-service:8080` (DNS interno, para a chamada REST de poder) e `Services__BattleServiceWsBaseUrl=ws://localhost:8083/ws` (URL entregue ao cliente, que roda no host via porta publicada 8083). No Kubernetes, o `Services__BattleServiceWsBaseUrl` do ConfigMap é um placeholder: sem Ingress, ajuste-o ao acesso externo real do battle-service.

## Kubernetes

Manifests em `deploy/k8s/<serviço>/`, aplicados com Kustomize:

```bash
kubectl apply -k deploy/k8s/auth-service
kubectl apply -k deploy/k8s/inventory-service
kubectl apply -k deploy/k8s/matchmaking-service
kubectl apply -k deploy/k8s/battle-service
kubectl get all -n onlinegame
```

Namespace único: `onlinegame`. Imagens esperadas: `onlinegame/auth-service:baseline`, `onlinegame/inventory-service:baseline`, `onlinegame/matchmaking-service:baseline`, `onlinegame/battle-service:baseline` (`imagePullPolicy: IfNotPresent`).

### Componentes por serviço
Cada pasta de serviço C# contém: `namespace.yaml`, `configmap.yaml`, `*-deployment.yaml`, `*-service.yaml`, `postgres-deployment.yaml`, `postgres-service.yaml` e `kustomization.yaml`. O auth adiciona `secret.yaml`, `redis-deployment.yaml` e `redis-service.yaml`. O `battle-service` não tem Postgres (só configmap + deployment + service).

- **Deployments:** 1 réplica, container na porta 8080, `readinessProbe`/`livenessProbe` via `GET /swagger/index.html` (serviços C#) ou `GET /healthz` (battle-service).
- **auth:** injeta config via `configMapRef` (auth-service-config) **e** `secretRef` (auth-service-secret com `JWT_SECRET`).
- **inventory / matchmaking / battle:** injetam apenas `configMapRef` (segredos ainda em ConfigMap - ponto de hardening).
- **battle-service:** o Service nomeia a porta `http-ws` (`appProtocol: http`) para o Istio suportar o upgrade de WebSocket; usa `redis` e `inventory-service` (gRPC) do mesmo namespace.

### Feature flag de criptografia (mTLS)

A criptografia do tráfego entre serviços é uma feature flag opcional via Istio, em `deploy/k8s/mesh/` (`PeerAuthentication` `STRICT`/`DISABLE`). Não faz parte da baseline e serve aos experimentos de overhead. Cobre os fluxos east-west **battle → inventory (gRPC)** e **matchmaking → inventory (REST)**; o WebSocket externo do battle exige a exceção `peer-authentication-battle-ws.yaml`. Guia completo, análise de cobertura e metodologia em [`deploy/k8s/mesh/README.md`](../deploy/k8s/mesh/README.md) e no [roadmap de segurança](./roadmap-seguranca.md).

### Observações da baseline (do `deploy/README.md`)
- Segredos e conexões locais em texto claro **por design** (baseline Unprotected).
- Atualizar `deploy/k8s/auth-service/secret.yaml` antes de qualquer ambiente compartilhado.

## Imagem Docker (comum aos 3 serviços)

Dockerfile multi-stage idêntico em estrutura:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./src ./src
WORKDIR /src/src/Api
RUN dotnet restore "<Serviço>.Api.csproj"
RUN dotnet publish "<Serviço>.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "<Serviço>.Api.dll"]
```

Build framework-dependent (`UseAppHost=false`) para imagem menor. Runtime sem SDK.

## Portas em desenvolvimento (fora de container)

Ao rodar via `dotnet run` (perfis em `launchSettings.json`):

| Serviço | HTTP | HTTPS |
|---------|------|-------|
| auth-service | 5179 | 7138 |
| inventory-service | 5017 | 7189 |
| matchmaking-service | 5068 | 7259 |

O battle-service (Go) roda com `go run ./cmd/battle-service`, escutando em `:8080` (`HTTP_ADDR`). Note que seu `INVENTORY_GRPC_ADDR` default no código é `localhost:5017` (a porta HTTP dev do inventory), útil para rodar tudo fora de container.
