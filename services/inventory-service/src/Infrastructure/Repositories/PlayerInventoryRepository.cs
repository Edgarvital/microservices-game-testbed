using OnlineGame.InventoryService.Core.Domain.Entities;
using OnlineGame.InventoryService.Core.Application.Repositories;
using OnlineGame.InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace OnlineGame.InventoryService.Infrastructure.Repositories;

public class PlayerInventoryRepository : IPlayerInventoryRepository
{
    private readonly InventoryDbContext _db;
    public PlayerInventoryRepository(InventoryDbContext db) => _db = db;

    public async Task<PlayerInventory?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default)
        => await _db.PlayerInventories
            .Include(pi => pi.Cards)
            .ThenInclude(pc => pc.Card)
            .Include(pi => pi.Decks)
            .ThenInclude(d => d.Slots)
            .FirstOrDefaultAsync(pi => pi.PlayerId == playerId, cancellationToken);

    public async Task AddAsync(PlayerInventory inventory, CancellationToken cancellationToken = default)
    {
        _db.PlayerInventories.Add(inventory);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlayerInventory inventory, CancellationToken cancellationToken = default)
    {
        _db.PlayerInventories.Update(inventory);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

