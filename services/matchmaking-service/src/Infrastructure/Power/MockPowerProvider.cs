using OnlineGame.MatchmakingService.Core.Application.Power;
using OnlineGame.MatchmakingService.Infrastructure.ExternalServices;

namespace OnlineGame.MatchmakingService.Infrastructure.Power;

/// <summary>
/// Provider de poder que tenta consultar o inventory-service real.
/// Fallback para mock se o serviço externo não responder.
/// </summary>
public sealed class MockPowerProvider : IPowerProvider
{
    private readonly InventoryServiceClient _inventoryClient;

    public MockPowerProvider(InventoryServiceClient inventoryClient)
    {
        _inventoryClient = inventoryClient;
    }

    public async Task<int> GetPowerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        // Tentar obter poder real do inventory-service
        var realPower = await _inventoryClient.GetPlayerPowerAsync(playerId, cancellationToken);
        
        if (realPower.HasValue)
        {
            return realPower.Value;
        }

        // Fallback: poder fixo de teste (Bronze arena)
        return 250;
    }
}
