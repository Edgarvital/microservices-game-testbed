# OnlineGame - Testbed de Microsserviços

Testbed de microsserviços para um jogo multiplayer online baseado em cartas, usado como artefato prático da pesquisa de mestrado sobre **segurança em arquiteturas de microsserviços orquestradas por Kubernetes**.

O sistema implementa o backend de um jogo: autenticação, inventário de cartas e matchmaking em **C# (.NET 8)** seguindo Clean Architecture, e um servidor de partidas em tempo real em **Go**. Deploy local via Docker Compose e manifests de Kubernetes. O código está deliberadamente na sua linha de base **"Unprotected"** (Fase 1): comunicação em texto claro, segredos hardcoded e sem políticas de rede. Essa base serve de ponto de partida para aplicar e medir as contramedidas criptográficas estudadas na Revisão Sistemática da Literatura (mTLS, gestão dinâmica de segredos com Vault, network policies).

## Índice da documentação

| Documento | Conteúdo |
|-----------|----------|
| [Visão geral e arquitetura](./visao-geral.md) | Contexto, diagrama de componentes, fluxos ponta a ponta, decisões transversais |
| [auth-service](./services/auth-service.md) | Autenticação (guest/registrado), emissão de JWT, integração com Vault |
| [inventory-service](./services/inventory-service.md) | Inventário, cartas, decks, poder do jogador; expõe REST + gRPC |
| [matchmaking-service](./services/matchmaking-service.md) | Pareamento por arena/poder, filas em Redis, worker de background |
| [battle-service](./services/battle-service.md) | Partidas 1v1 em tempo real (Go), WebSocket, gRPC e Redis Pub/Sub |
| [Infraestrutura e deploy](./infraestrutura-deploy.md) | Docker Compose, Kubernetes, portas, bancos, Redis |
| [Roadmap de segurança](./roadmap-seguranca.md) | Da baseline Unprotected às contramedidas da RSL |
| [Contexto de mestrado](./Mestrado/main.md) | Artigo da Revisão Sistemática da Literatura |

## Componentes

| Serviço | Linguagem | Porta local (Compose) | Banco | Dependências |
|---------|-----------|-----------------------|-------|--------------|
| auth-service | C# / .NET 8 | 8080 | PostgreSQL (`onlinegame_auth`, :5432) | Redis, (Vault opcional) |
| inventory-service | C# / .NET 8 | 8081 | PostgreSQL (`onlinegame_inventory`, :5433) | - |
| matchmaking-service | C# / .NET 8 | 8082 | PostgreSQL (`onlinegame_matchmaking`, :5434) | Redis, inventory-service |
| battle-service | Go 1.21 | 8083 | (nenhum) | Redis, inventory-service (gRPC) |

> **Frontend:** o `.gitignore` reserva espaço para um frontend em Godot, ainda não presente no repositório.

## Início rápido

```bash
cd deploy
docker compose up -d --build
```

- auth-service: http://localhost:8080/swagger
- inventory-service: http://localhost:8081/swagger
- matchmaking-service: http://localhost:8082/swagger
- battle-service: `ws://localhost:8083/ws` (WebSocket) + `http://localhost:8083/healthz`

Detalhes completos em [Infraestrutura e deploy](./infraestrutura-deploy.md).

## Convenções comuns aos serviços C#

Aplicam-se aos três serviços .NET (auth, inventory, matchmaking). O battle-service (Go) segue as próprias convenções, descritas em sua [página](./services/battle-service.md).

- **Clean Architecture** em 4 projetos: `Api` -> `Infrastructure` -> `Core/Application` -> `Core/Domain`. As dependências apontam sempre para o domínio.
- **Persistência:** EF Core 8 + Npgsql (PostgreSQL). Migrations aplicadas automaticamente no boot (`MigrateAsync`), seguidas de seeders idempotentes.
- **API:** ASP.NET Core Controllers, Swagger/OpenAPI em Development, `GlobalExceptionHandler` + `ProblemDetails`, e um contrato de erro padronizado `ApiErrorResponse` (`code` / `message` / `details`).
- **Container:** Dockerfile multi-stage (`sdk:8.0` para build, `aspnet:8.0` para runtime), expondo a porta interna **8080**.
</content>
</invoke>
