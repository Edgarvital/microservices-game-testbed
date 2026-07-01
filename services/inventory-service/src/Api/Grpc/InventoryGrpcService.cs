using Grpc.Core;
using OnlineGame.InventoryService.Core.Application.Repositories;
using OnlineGame.Inventory.V1;

namespace OnlineGame.InventoryService.Api.Grpc;

public sealed class InventoryGrpcService : global::OnlineGame.Inventory.V1.InventoryService.InventoryServiceBase
{
    private readonly IPlayerInventoryRepository _playerInventoryRepository;

    public InventoryGrpcService(IPlayerInventoryRepository playerInventoryRepository)
    {
        _playerInventoryRepository = playerInventoryRepository;
    }

    public override async Task<GetBattleStatsResponse> GetBattleStats(GetBattleStatsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.PlayerId, out var playerId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "playerId must be a valid GUID."));
        }

        var inventory = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, context.CancellationToken);
        if (inventory is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Player inventory was not found."));
        }

        var maxHp = inventory.BaseHp + (inventory.Power / 2) + (inventory.BaseLevel * 10);
        var baseDamage = inventory.BaseAttack + (inventory.Power / 4);
        var moveSpeed = 5.0 + (inventory.BaseLevel * 0.2);

        return new GetBattleStatsResponse
        {
            MaxHp = maxHp,
            BaseDamage = baseDamage,
            MoveSpeed = moveSpeed
        };
    }
}
