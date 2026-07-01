# matchmaking-service

Empareha jogadores em partidas por **arena** (faixa de poder). Usa PostgreSQL para as arenas fixas e **Redis** para filas e status efêmero. Consulta o inventory-service para obter o poder do jogador e roda um worker de background que forma os pares.

- **Porta interna:** 8080 (Compose: `http://localhost:8082`)
- **Banco:** PostgreSQL `onlinegame_matchmaking` + Redis
- **Solution:** `services/matchmaking-service/OnlineGame.MatchmakingService.sln`

> **Atenção (baseline):** este serviço **não tem autenticação**. `Program.cs` chama `UseAuthorization()` mas não registra esquema de autenticação e os controllers não têm `[Authorize]`. É um alvo explícito de hardening (ver [roadmap](../roadmap-seguranca.md)).

## Responsabilidades

- Receber pedidos de entrada na fila (`join`).
- Obter o poder do jogador (via inventory, com fallback).
- Selecionar a arena adequada pela faixa de poder.
- Enfileirar o jogador no Redis e registrar seu status.
- Emparelhar 2 jogadores da mesma arena em background.
- Reportar o status (Queued / Matched).

## Modelo de domínio

### `Arena`
`src/Core/Domain/Entities/Arena.cs`: `Id`, `Name`, `MinPower`, `MaxPower?` (nulo = sem teto). `AcceptsPower(power)` -> `power >= MinPower && (MaxPower is null || power <= MaxPower)`.

Arenas semeadas (`ArenaDataSeeder`):

| Id | Nome | MinPower | MaxPower |
|----|------|----------|----------|
| 1111...1111 | Arena Bronze | 0 | 400 |
| 2222...2222 | Arena Silver | 401 | 800 |
| 3333...3333 | Arena Gold | 801 | (sem teto) |

### Tipos da aplicação
- `MatchmakingTicket(PlayerId, Power, JoinedAtUtc)` - entrada na fila.
- `MatchmakingStatus` (enum): `Queued=0`, `Matched=1`.
- `MatchmakingPlayerStatus(PlayerId, Status, ArenaId, ArenaName, Power, MatchId?, OpponentId?, UpdatedAtUtc)` - persistido no Redis (TTL 30 min).
- `JoinMatchmakingCommand(PlayerId)` / `JoinMatchmakingResult(PlayerId, ArenaId, ArenaName, Power, QueueKey, QueuePosition)`.

## Endpoints

Base: `/api/matchmaking`.

| Método | Rota | Request | Sucesso | Erros |
|--------|------|---------|---------|-------|
| POST | `/join` | `{playerId}` | 200 `JoinMatchmakingResponse` | 400 `VALIDATION_ERROR` |
| GET | `/status/{playerId}` | - | 200 `MatchmakingStatusResponse` | 404 `MATCHMAKING_STATUS_NOT_FOUND` |

`JoinMatchmakingResponse`: `playerId`, `power`, `arenaId`, `arenaName`, `queueKey`, `queuePosition`.
`MatchmakingStatusResponse`: `playerId`, `status`, `arenaId`, `arenaName`, `power`, `matchId?`, `battleWsUrl?`, `opponentId?`, `updatedAtUtc`.

## Fluxo interno

### Entrada na fila (`JoinMatchmakingHandler`)
1. `IPowerProvider.GetPowerAsync(playerId)` -> poder do jogador.
2. `IArenaRepository.FindByPowerAsync(power)` -> arena (senão `InvalidOperationException`).
3. `IMatchmakingQueue.EnqueueAsync(arenaId, ticket)` -> `RPUSH`, devolve posição.
4. `IMatchmakingStatusStore.SetQueuedAsync(...)` -> grava status `Queued` (TTL 30 min).

### Worker de pareamento (`MatchmakingPairingWorker`, `IHostedService`)
Loop a cada **1 segundo**:
- Lista as arenas com fila (`GetArenaIdsAsync` via `KEYS matchmaking:arena:*`).
- Enquanto a fila tiver `>= 2`: `LPOP` dois tickets, gera `matchId` (`Guid.NewGuid()`), grava status `Matched` para ambos (cada um recebe o outro como `OpponentId`).
- **Publica um `MatchFoundEvent` no canal Redis `battle:match-found`** (`PUBLISH`), que dispara a criação da partida no [battle-service](battle-service.md). Payload JSON: `matchId`, `arenaId`, `arenaName`, `playerAId`, `playerBId`, `playerAPower`, `playerBPower`.

O pareamento é FIFO por arena; hoje não valida diferença de poder entre os dois além de estarem na mesma faixa.

### Descoberta do servidor de batalha
O `GET /status/{playerId}` devolve, quando a partida foi formada, o campo `battleWsUrl` (montado a partir de `Services:BattleServiceWsBaseUrl`, default `ws://localhost:8083/ws`) com `matchId` e `playerId` na query. É assim que o cliente sabe onde conectar o WebSocket.

### Redis
| Chave | Tipo | Uso |
|-------|------|-----|
| `matchmaking:arena:{arenaId:N}` | LIST | fila FIFO de tickets (JSON) |
| `matchmaking:player:{playerId:N}` | STRING (TTL 30min) | `MatchmakingPlayerStatus` (JSON) |
| `battle:match-found` | Pub/Sub | canal de eventos de partida para o battle-service |

Operações: `RPUSH` (enqueue), `LPOP` (dequeue), `LLEN` (tamanho), `KEYS` (arenas ativas), `PUBLISH` (evento de partida).

## Integração com inventory-service

`IPowerProvider` -> `MockPowerProvider`:
1. Chama `InventoryServiceClient.GetPlayerPowerAsync(playerId)` -> `GET /api/inventory/{playerId}`, lê `Power`.
2. Em qualquer falha (timeout, 404, exceção) retorna **fallback 250** (faixa Bronze).

URL do inventory em `Services:InventoryServiceUrl` (Dev: `http://localhost:8081`; prod/appsettings: `http://localhost:5001`). Registrado via `AddHttpClient<InventoryServiceClient>`.

> A chamada é HTTP em texto claro e sem autenticação entre serviços: exatamente o tipo de comunicação *east-west* que a pesquisa propõe proteger com mTLS.

## Persistência

`MatchmakingDbContext` com `Arena` (config em `ArenaConfiguration`: tabela `Arenas`, `Name` <=100, `MaxPower` opcional). Migration `20260424152848_InitialCreate`. `DatabaseSeeder` aplica migrations pendentes e chama `ArenaSeeder` (idempotente). Redis é usado só para dados dinâmicos/efêmeros.

## Configuração

| Chave | Dev | Observação |
|-------|-----|------------|
| `ConnectionStrings:MatchmakingDb` | Postgres local | env `ConnectionStrings__MatchmakingDb` |
| `ConnectionStrings:Redis` | `localhost:6379` / `redis:6379` | env `ConnectionStrings__Redis` |
| `Services:InventoryServiceUrl` | `http://localhost:8081` | URL do inventory |

DI (`DependencyInjection.cs`): `DbContext` (Npgsql); `IConnectionMultiplexer` Singleton (StackExchange.Redis); `InventoryServiceClient` via HttpClient; Scoped `IArenaRepository`, `IMatchmakingQueue` (`RedisMatchmakingQueue`), `IMatchmakingStatusStore` (`RedisMatchmakingStatusStore`), `IPowerProvider` (`MockPowerProvider`), `IJoinMatchmakingHandler`; e `MatchmakingPairingWorker` como HostedService.

## Dependências principais

`Microsoft.EntityFrameworkCore` 8.0.0, `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.0, `StackExchange.Redis` 2.7.33, `Microsoft.Extensions.Http`, `Microsoft.Extensions.Hosting.Abstractions`, `Swashbuckle.AspNetCore` 6.4.0.

## Build (Docker)

Multi-stage `sdk:8.0` -> `aspnet:8.0`, porta 8080, entry `OnlineGame.MatchmakingService.Api.dll`.
</content>
