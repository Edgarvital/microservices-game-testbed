# auth-service

Serviço de identidade do ecossistema. Cria contas, autentica jogadores e **emite** os tokens JWT usados pelos demais serviços. É o único que integra com o HashiCorp Vault (opcional) para obter o segredo de assinatura.

- **Porta interna:** 8080 (Compose: `http://localhost:8080`)
- **Banco:** PostgreSQL `onlinegame_auth`
- **Solution:** `services/auth-service/OnlineGame.AuthService.sln`

## Responsabilidades

- Autenticação de **convidados (guest)** via `deviceId`.
- Registro e login de contas **registradas** via `email` + `passwordHash`.
- **Promoção** guest -> registrado (`link-account`), preservando o `Id`.
- Emissão de JWT (HS256) e registro do último login.
- Garantia de unicidade de `deviceId` e `email`.

## Modelo de domínio: `User`

`src/Core/Domain/User.cs`. A entidade foi renomeada de `Player` para `User` (ver histórico), mas a tabela no banco continua `players` e o identificador é exposto como `playerId` na API.

| Campo | Tipo | Observação |
|-------|------|------------|
| `Id` | `Guid` | Identidade; mantida na promoção guest -> registrado |
| `DeviceId` | `string?` | Presente em guests (e híbridos) |
| `Email` | `string?` | Presente em registrados |
| `PasswordHash` | `string?` | Hash enviado pelo cliente (o servidor não faz hashing) |
| `CreatedAt` / `LastLoginAt` | `DateTime` | UTC |
| `IsGuest` | `bool` (derivado) | `true` quando `Email` é vazio; não é mapeado no banco |

Métodos de fábrica: `CreateGuest(deviceId)`, `CreateRegistered(email, passwordHash)`, `Rehydrate(...)`. Comportamentos: `LinkAccount(email, passwordHash)` (exige ser guest, senão `InvalidOperationException`) e `RecordLogin()`.

**Estados possíveis:**

| | Guest | Registrado | Híbrido (após link) |
|-|:-----:|:----------:|:-------------------:|
| `DeviceId` | obrigatório | null | obrigatório |
| `Email` / `PasswordHash` | null | obrigatório | obrigatório |
| `IsGuest` | true | false | false |

## Endpoints

Base: `/api/auth`. Todos anônimos (é o ponto de entrada de identidade). Corpo de erro no formato `ApiErrorResponse`.

| Método | Rota | Request | Sucesso | Erros principais |
|--------|------|---------|---------|------------------|
| POST | `/guest/register` | `{deviceId}` | 201 | 409 `DEVICE_ALREADY_REGISTERED` |
| POST | `/guest/login` | `{deviceId}` | 200 | 404 `GUEST_NOT_FOUND`, 401 `DEVICE_LINKED_TO_REGISTERED` |
| POST | `/guest/link-account` | `{deviceId, email, passwordHash}` | 200 | 404 `GUEST_NOT_FOUND`, 409 `EMAIL_ALREADY_LINKED` |
| POST | `/registered/register` | `{email, passwordHash}` | 201 | 409 `EMAIL_ALREADY_REGISTERED` |
| POST | `/registered/login` | `{email, passwordHash}` | 200 | 404 `REGISTERED_NOT_FOUND`, 401 `INVALID_CREDENTIALS` |

Validação de entrada por Data Annotations (ex.: `deviceId` mínimo 3 chars, `passwordHash` mínimo 8, `email` válido). Falhas de validação retornam 400 `VALIDATION_ERROR` com `details` por campo.

**Resposta (`AuthResponse`):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiI...",
  "expiresAtUtc": "2026-06-30T02:00:00Z",
  "playerId": "550e8400-e29b-41d4-a716-446655440000",
  "isGuest": true,
  "email": null,
  "deviceId": "device-001"
}
```

## Segurança

### Emissão de JWT
`src/Infrastructure/Security/JwtTokenGenerator.cs`. Algoritmo **HS256**, exige segredo de no mínimo 32 bytes. TTL configurável (`Jwt:AccessTokenTtlMinutes`, padrão 120 min).

Claims emitidos:

| Claim | Valor |
|-------|-------|
| `sub` | `playerId` (usado pelo inventory para autorização) |
| `jti` | id único do token |
| `is_guest` | `"true"`/`"false"` |
| `email` | se registrado |
| `device_id` | se guest/híbrido |

### Comparação de senha
O servidor **não faz hashing**. Compara `PasswordHash` recebido com o armazenado via `string.Equals(..., Ordinal)`. O cliente é responsável por aplicar um hash forte (bcrypt/argon2/PBKDF2) antes de enviar. A comparação ordinal não é constant-time (candidato a hardening).

### Provedores de segredo (`ISecretProvider`)
Selecionado por `Secrets:Provider`:

- **`Local`** (padrão) - `LocalSecretProvider`: lê `JWT_SECRET` (env) e, como fallback, `Jwt:Secret` (config).
- **`Vault`** - `VaultSecretProvider`: busca o segredo no HashiCorp Vault via API KV v2 (`GET v1/{mount}/data/{path}`, header `X-Vault-Token`). Configurado em `Vault:*` (`Address`, `Token`, `MountPath`, `SecretPath`, `JwtSecretField`).

O provedor Vault é a base para a contramedida de **gestão dinâmica de segredos** discutida na pesquisa.

## Persistência

`src/Infrastructure/Persistence/AuthDbContext.cs`. Tabela `players`, mapeamento snake_case, `IsGuest` ignorado. Índices únicos **parciais**:

```sql
CREATE UNIQUE INDEX ux_players_email     ON players(email)     WHERE email     IS NOT NULL;
CREATE UNIQUE INDEX ux_players_device_id ON players(device_id) WHERE device_id IS NOT NULL;
```

Repositório: `PlayerRepository` (implementa `IPlayerRepository`) com `GetByDeviceIdAsync`, `GetByEmailAsync`, `AddAsync`, `UpdateAsync`. Migration inicial: `20260423172327_InitialCreate`. Migrations aplicadas no boot por `InitializeInfrastructureAsync` (`MigrateAsync`).

Connection string: `AUTH_DB_CONNECTION` (env) tem prioridade sobre `ConnectionStrings:AuthDb`.

## Configuração

| Chave | Origem | Padrão |
|-------|--------|--------|
| `AUTH_DB_CONNECTION` / `ConnectionStrings:AuthDb` | env / appsettings | conexão local Postgres |
| `Secrets:Provider` | appsettings / env | `Local` |
| `JWT_SECRET` / `Jwt:Secret` | env / appsettings | segredo baseline (trocar!) |
| `Jwt:AccessTokenTtlMinutes` | appsettings | 120 |
| `Vault:*` | appsettings | endpoint/token do Vault |

`Program.cs`: registra infraestrutura, controllers, Swagger (Development), `GlobalExceptionHandler` + ProblemDetails, aplica migrations e sobe o pipeline (`UseHttpsRedirection` -> `UseAuthorization` -> `MapControllers`). Note que **não** há `AddAuthentication/JwtBearer`: o serviço só emite tokens.

DI (`Infrastructure/DependencyInjection.cs`): `DbContext` Scoped, `ISecretProvider` Singleton, `IPlayerRepository` Scoped, `IJwtTokenGenerator` Singleton.

## Dependências principais

`Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4, `Microsoft.EntityFrameworkCore` 8.0.5, `System.IdentityModel.Tokens.Jwt` 7.5.1, `Swashbuckle.AspNetCore` 6.4.0.

## Build (Docker)

Multi-stage: `sdk:8.0` (restore + publish Release) -> `aspnet:8.0`. Expõe 8080, `ASPNETCORE_ENVIRONMENT=Development`. Entry: `OnlineGame.AuthService.Api.dll`.
