# battle-service

Servidor de **partidas 1v1 em tempo real**, escrito em **Go**. Recebe eventos de partida do matchmaking (via Redis Pub/Sub), busca os atributos de combate dos jogadores no inventory (via gRPC) e roda o game loop autoritativo, transmitindo o estado aos clientes por WebSocket.

- **Porta interna:** 8080 (Compose: `http://localhost:8083`, WebSocket em `/ws`)
- **Sem banco próprio** (usa Redis compartilhado + inventory via gRPC)
- **Módulo Go:** `onlinegame/battle-service` (Go 1.21)

## Responsabilidades

- Consumir eventos `match-found` do matchmaking (Redis Pub/Sub).
- Buscar stats de combate (HP, dano, velocidade) de cada jogador no inventory (gRPC).
- Criar e simular salas de jogo (`GameRoom`) a 30 ticks/s.
- Processar inputs dos jogadores (mover, lançar feitiço), física de projéteis e colisões.
- Transmitir snapshots do estado a ambos os clientes por WebSocket.

## Estrutura (Go)

```
battle-service/
├── cmd/battle-service/main.go   # boot e wiring das dependências
├── internal/
│   ├── domain/                  # GameRoom, Player, Projectile, snapshots, regras
│   ├── engine/                  # game loop (loop.go) e RoomManager (manager.go)
│   ├── network/                 # server.go (HTTP->WebSocket) e hub.go (conexões)
│   └── gateway/                 # integração externa
│       ├── config.go            # config via env
│       ├── grpc_client.go       # cliente gRPC do inventory
│       ├── listener.go          # subscriber Redis (match-found)
│       └── inventorypb/         # descritores gRPC (dynamic protobuf)
├── go.mod
└── Dockerfile
```

## Boot (`main.go`)

1. Carrega config de env (`LoadConfigFromEnv`).
2. Cria client Redis (`REDIS_ADDR`).
3. Abre conexão gRPC com o inventory (`INVENTORY_GRPC_ADDR`).
4. Cria `RoomManager` (recebe o gRPC client como `BattleStatsProvider`) e `Hub`.
5. Sobe o `MatchFoundListener` (goroutine) escutando o canal Redis.
6. Registra rotas HTTP: `/ws` (WebSocket) e `/healthz` (health).
7. Sobe o HTTP server; shutdown gracioso em SIGINT/SIGTERM (timeout 5s).

## Domínio e engine

### Entidades (`internal/domain/types.go`)
- **Player:** `ID`, `Position`/`TargetPosition` (Vector2D), `MoveSpeed`, `HP`/`MaxHP`, `BaseDamage`.
- **Projectile:** `ID`, `OwnerID`, `Position`, `Velocity`, `Damage`, `Radius`.
- **GameRoom:** `MatchID`, `ArenaID/Name`, mapa de `Players`, lista de `Projectiles`, canais `Inputs` e `Snapshots`, mutex.

### Game loop (`internal/engine/loop.go`)
- **30 ticks/s** (`dt ≈ 0.0333s`). A cada tick (`stepRoom`): drena inputs (mover / lançar feitiço), atualiza posições e projéteis, detecta colisões (reduz HP, remove projétil), gera e publica snapshot.
- Regras: movimento suave até `TargetPosition` limitado por `MoveSpeed`; feitiço cria projétil normalizado a 18 px/tick; acerto se distância <= `Radius + 0.8`; HP com clamp em 0; jogador não é atingido pelo próprio projétil.

### RoomManager (`internal/engine/manager.go`)
`CreateOrGet(event)`: busca stats dos dois jogadores no inventory via gRPC (em paralelo), cria os `Player`s, monta a `GameRoom` e inicia o `RunRoomLoop` em goroutine. Se o gRPC falhar, a criação da sala falha.

## Comunicação (crítico para a feature flag)

### Entrada
| Origem | Protocolo | Porta | Detalhe | Env |
|--------|-----------|-------|---------|-----|
| Redis (do matchmaking) | Pub/Sub (TCP) | 6379 | canal `battle:match-found`, payload JSON `MatchFoundEvent` | `REDIS_ADDR`, `MATCH_CHANNEL` |
| Clientes | WebSocket (HTTP/1.1 upgrade) | 8080 | `/ws?matchId=X&playerId=Y`; inputs JSON | `HTTP_ADDR` |

### Saída
| Destino | Protocolo | Porta | Detalhe | Env |
|---------|-----------|-------|---------|-----|
| inventory-service | **gRPC (HTTP/2)** | 8080 (Compose usa `inventory-service:8080`) | `InventoryService/GetBattleStats`; **`insecure` (sem TLS)** | `INVENTORY_GRPC_ADDR` |
| Clientes | WebSocket | 8080 | broadcast de `GameStateSnapshot` (JSON) | `HTTP_ADDR` |

> **Nota:** o `INVENTORY_GRPC_ADDR` default no código é `localhost:5017`, mas no Docker Compose é sobrescrito para `inventory-service:8080`. O gRPC usa `insecure.NewCredentials()` na aplicação: a criptografia desse salto será provida pelo mTLS do mesh (ver [feature flag](../../deploy/k8s/mesh/README.md)), não pelo código.

### `MatchFoundEvent` (contrato Redis)
Publicado pelo matchmaking (`MatchmakingPairingWorker`), consumido pelo battle:
```json
{ "matchId": "...", "arenaId": "...", "arenaName": "...",
  "playerAId": "...", "playerBId": "...", "playerAPower": 0, "playerBPower": 0 }
```

## Variáveis de ambiente

| Var | Default (código) | Compose |
|-----|------------------|---------|
| `HTTP_ADDR` | `:8080` | `:8080` |
| `REDIS_ADDR` | `localhost:6379` | `redis:6379` |
| `INVENTORY_GRPC_ADDR` | `localhost:5017` | `inventory-service:8080` |
| `MATCH_CHANNEL` | `battle:match-found` | `battle:match-found` |

## Dependências (`go.mod`)

`github.com/gorilla/websocket` v1.5.3, `github.com/redis/go-redis/v9` v9.6.1, `google.golang.org/grpc` v1.74.0, `google.golang.org/protobuf` v1.36.6.

O cliente gRPC não usa stubs gerados por `protoc`: monta os descritores em runtime (dynamic protobuf) em `internal/gateway/inventorypb/descriptor.go`. O lado servidor (inventory) usa `Grpc.Tools` sobre `proto/player_inventory.proto`.

## Build (Docker)

Multi-stage: `golang:1.23-alpine` (build) -> `alpine:3.20` (runtime, binário ~20MB). Expõe 8080. Entry: `/app/battle-service`.

## Segurança (estado baseline)

Coerente com a linha de base Unprotected: gRPC `insecure`, Redis sem auth, WebSocket sem TLS, `CheckOrigin` aceita qualquer origem. O endurecimento da comunicação é feito na camada de infraestrutura (mTLS via mesh) sem alterar o serviço. Ver [roadmap de segurança](../roadmap-seguranca.md).
