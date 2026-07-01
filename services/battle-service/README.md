# battle-service

Serviço Go responsável pelo tempo real do jogo 1v1.

## Rodar localmente

```bash
cd services/battle-service
go test ./...
go run ./cmd/battle-service
```

## Variáveis de ambiente

- `HTTP_ADDR` default `:8080`
- `REDIS_ADDR` default `localhost:6379`
- `INVENTORY_GRPC_ADDR` default `localhost:5017`
- `MATCH_CHANNEL` default `battle:match-found`
