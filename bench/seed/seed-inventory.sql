-- Semeia um inventario de teste deterministico para o benchmark.
-- Sem isso, o gRPC GetBattleStats retorna NOT_FOUND (caminho de erro, mais curto),
-- o que distorce a medicao. Com o inventario existente, medimos o caminho feliz.
--
-- Executar contra o banco onlinegame_inventory. Ex. (via docker-compose):
--   docker exec -i onlinegame-inventory-db psql -U postgres -d onlinegame_inventory < bench/seed/seed-inventory.sql
-- Ou no cluster:
--   kubectl exec -i deploy/inventory-db -n onlinegame -- psql -U postgres -d onlinegame_inventory < bench/seed/seed-inventory.sql
--
-- O PLAYER_ID abaixo deve casar com o usado no benchmark (default do run.sh).
-- Confira os nomes de coluna com o snapshot do EF se este INSERT falhar.

INSERT INTO "PlayerInventories" ("PlayerId", "BaseLevel", "BaseHp", "BaseAttack", "Power", "LastUpdate")
VALUES ('11111111-1111-1111-1111-111111111111', 5, 100, 10, 250, now() AT TIME ZONE 'utc')
ON CONFLICT ("PlayerId") DO UPDATE
  SET "BaseLevel" = EXCLUDED."BaseLevel",
      "BaseHp" = EXCLUDED."BaseHp",
      "BaseAttack" = EXCLUDED."BaseAttack",
      "Power" = EXCLUDED."Power",
      "LastUpdate" = EXCLUDED."LastUpdate";
