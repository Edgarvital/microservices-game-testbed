using OnlineGame.InventoryService.Core.Domain.Entities;

namespace OnlineGame.InventoryService.Core.Application.Repositories;

public interface IPlayerInventoryRepository
{
    Task<PlayerInventory?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task AddAsync(PlayerInventory inventory, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlayerInventory inventory, CancellationToken cancellationToken = default);
}

