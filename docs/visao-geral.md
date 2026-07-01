# Visão geral e arquitetura

## Contexto

O projeto é o backend de um jogo multiplayer online baseado em cartas. Ele existe como **testbed** para a pesquisa de mestrado cujo tema é mitigar vulnerabilidades em microsserviços orquestrados por Kubernetes com contramedidas criptográficas (mTLS, rotação de segredos, network policies). Ver [contexto da RSL](./Mestrado/RSL%20Parcial/main.md).

O estado atual do código é a **linha de base "Unprotected"** descrita no `deploy/README.md`: segredos e conexões em texto claro por design. Isso é intencional: representa o "antes" contra o qual as contramedidas serão aplicadas e medidas (por exemplo com Kube-bench / Kube-hunter).

## Princípios de arquitetura

Os três serviços C# (auth, inventory, matchmaking) seguem **Clean Architecture** com quatro camadas em projetos separados. O battle-service (Go) usa o layout idiomático `cmd/` + `internal/` (domain/engine/network/gateway), com a mesma intenção de isolar domínio das dependências externas.

```
Api             (apresentação: controllers, DTOs, middleware, Program.cs)
  -> Infrastructure   (EF Core, repositórios, Redis, clients HTTP, DI)
       -> Core/Application  (interfaces/casos de uso; sem dependências externas)
            -> Core/Domain      (entidades e regras de negócio; C# puro)
```

Regras:
- Dependências apontam sempre para o centro (domínio). O domínio não conhece EF, Redis nem HTTP.
- As interfaces (repositórios, provedores) vivem em `Core/Application`; as implementações concretas em `Infrastructure`.
- A camada `Api` é fina: valida entrada, chama a aplicação/infraestrutura e mapeia para DTOs.

Cada serviço C# tem **banco de dados próprio** (database-per-service); o battle-service não tem banco (é stateful só em memória durante a partida). Não há acesso cruzado a bancos. A integração entre serviços usa três mecanismos: **REST** (matchmaking → inventory), **gRPC** (battle → inventory) e **Redis Pub/Sub** (matchmaking → battle).

## Diagrama de componentes

```mermaid
flowchart TB
    subgraph client[Cliente do jogo]
        C[App / Godot - futuro]
    end

    subgraph auth[auth-service :8080]
        AUTHDB[(PostgreSQL onlinegame_auth)]
        VAULT{{HashiCorp Vault - opcional}}
    end

    subgraph inv[inventory-service :8081]
        INVDB[(PostgreSQL onlinegame_inventory)]
    end

    subgraph mm[matchmaking-service :8082]
        MMDB[(PostgreSQL onlinegame_matchmaking)]
        WORKER[[MatchmakingPairingWorker - background]]
    end

    subgraph battle[battle-service :8083 - Go]
        ENGINE[[Game loop 30Hz]]
    end

    REDIS[(Redis)]

    C -->|POST /api/auth/*| auth
    C -->|Bearer JWT| inv
    C -->|POST /api/matchmaking/*| mm
    C -->|WebSocket /ws| battle

    auth --> AUTHDB
    auth -.->|Secrets:Provider=Vault| VAULT
    inv --> INVDB
    mm --> MMDB
    mm -->|GET /api/inventory/id - poder REST| inv
    mm -->|filas + status| REDIS
    WORKER -->|LPOP pares| REDIS
    WORKER -->|PUBLISH battle:match-found| REDIS
    REDIS -->|SUBSCRIBE match-found| battle
    battle -->|gRPC GetBattleStats| inv
```

## Papéis de segurança de cada serviço (estado atual)

| Serviço | Emite JWT | Valida JWT | Observação |
|---------|:---------:|:----------:|------------|
| auth-service | Sim | Não | Emite o token; os endpoints de login/registro são anônimos por natureza. O pipeline não registra `JwtBearer`. |
| inventory-service | Não | **Sim (REST)** | Endpoints REST exigem `Bearer` (HS256) e extraem `PlayerId` do claim `sub`. O endpoint **gRPC** `GetBattleStats` é **anônimo** (não passa por `[Authorize]`). |
| matchmaking-service | Não | **Não** | Ainda sem autenticação. `Program.cs` chama `UseAuthorization()` mas não há esquema de autenticação nem `[Authorize]`. Ponto de hardening. |
| battle-service | Não | **Não** | WebSocket autentica só por query params (`matchId`/`playerId`); chamada gRPC ao inventory é `insecure`. |

Os serviços C# compartilham, na baseline, o mesmo segredo simétrico HS256 (`CHANGE_ME_BASELINE_LOCAL_SECRET_32B` no Compose). Isso permite que o token emitido pelo auth seja validado pelo inventory.

> A adição do battle-service introduziu **gRPC** (battle → inventory) e **Redis Pub/Sub** (matchmaking → battle) como novos canais east-west. Isso amplia a superfície que a [feature flag de mTLS](./roadmap-seguranca.md) precisa cobrir; a análise de cobertura está no [README do mesh](../deploy/k8s/mesh/README.md).

## Fluxo ponta a ponta

### 1. Autenticação

```mermaid
sequenceDiagram
    participant Cli as Cliente
    participant Auth as auth-service
    participant DB as onlinegame_auth
    Cli->>Auth: POST /api/auth/guest/register {deviceId}
    Auth->>DB: cria User (guest)
    Auth->>Auth: GenerateToken (HS256, sub=playerId, is_guest=true)
    Auth-->>Cli: 201 {accessToken, playerId, expiresAtUtc}
```

O `playerId` retornado é a identidade usada nos demais serviços. Um guest pode depois virar conta registrada via `guest/link-account`, mantendo o mesmo `Id`.

### 2. Inventário (autenticado)

```mermaid
sequenceDiagram
    participant Cli as Cliente
    participant Inv as inventory-service
    Cli->>Inv: GET /api/inventory/{playerId} (Bearer JWT)
    Inv->>Inv: valida JWT, confere sub == playerId
    Inv-->>Cli: 200 {cards, decks, power, ...}
```

O `Power` do jogador é armazenado no inventário e é o insumo do matchmaking.

### 3. Matchmaking

```mermaid
sequenceDiagram
    participant Cli as Cliente
    participant MM as matchmaking-service
    participant Inv as inventory-service
    participant R as Redis
    participant W as PairingWorker

    Cli->>MM: POST /api/matchmaking/join {playerId}
    MM->>Inv: GET /api/inventory/{playerId} (poder)
    Inv-->>MM: power (ou fallback 250)
    MM->>MM: escolhe Arena por faixa de poder
    MM->>R: RPUSH fila da arena + status Queued (TTL 30min)
    MM-->>Cli: 200 {arena, queuePosition}

    loop a cada 1s
        W->>R: LPOP 2 jogadores da mesma arena
        W->>R: grava status Matched (matchId, opponentId)
    end

    Cli->>MM: GET /api/matchmaking/status/{playerId}
    MM->>R: lê status
    MM-->>Cli: Queued ou Matched {matchId, opponentId}
```

O pareamento é simples: junta 2 jogadores consecutivos da mesma arena. Ao formar o par, o worker publica um `MatchFoundEvent` no canal Redis `battle:match-found`, que dispara a criação da partida no battle-service.

### 4. Batalha em tempo real

```mermaid
sequenceDiagram
    participant W as matchmaking (worker)
    participant R as Redis
    participant B as battle-service
    participant Inv as inventory-service
    participant Cli as Clientes (A e B)

    W->>R: PUBLISH battle:match-found (MatchFoundEvent)
    R-->>B: evento de partida
    B->>Inv: gRPC GetBattleStats(playerA)
    B->>Inv: gRPC GetBattleStats(playerB)
    B->>B: cria GameRoom, inicia loop 30Hz
    Cli->>B: WebSocket /ws?matchId&playerId
    loop 30 ticks/s
        Cli->>B: inputs (Move / CastSpell)
        B-->>Cli: snapshot do estado
    end
```

O cliente descobre a URL do WebSocket pelo `GET /api/matchmaking/status/{playerId}`, que agora devolve `battleWsUrl` quando a partida foi formada.

## Stack tecnológica

- **.NET 8 / C# 12**, ASP.NET Core (Controllers + Swagger) nos três serviços de backend.
- **Go 1.21** no battle-service (gorilla/websocket, go-redis, gRPC).
- **Entity Framework Core 8** + **Npgsql** (PostgreSQL 16).
- **Redis 7** (StackExchange.Redis / go-redis) para filas, status efêmero e Pub/Sub de partidas.
- **gRPC** (HTTP/2) para o fluxo battle → inventory (`Grpc.AspNetCore` no servidor; dynamic protobuf no cliente Go). Contrato em `proto/player_inventory.proto`.
- **WebSocket** para o tempo real cliente ↔ battle.
- **HashiCorp Vault** (opcional) como provedor de segredos do auth-service.
- **Docker** (multi-stage) e **Kubernetes** (namespace `onlinegame`, Kustomize por serviço). mTLS opcional via **Istio** (feature flag).

## Estrutura do repositório

```
microservices-game-testbed/
├── deploy/
│   ├── docker-compose.yml        # stack local completa
│   ├── README.md
│   └── k8s/                      # manifests por serviço (kustomize)
│       ├── auth-service/
│       ├── inventory-service/
│       └── matchmaking-service/
├── proto/
│   └── player_inventory.proto    # contrato gRPC compartilhado (inventory <-> battle)
├── services/
│   ├── auth-service/
│   ├── inventory-service/
│   ├── matchmaking-service/
│   └── battle-service/           # Go
└── docs/                         # esta documentação
```
