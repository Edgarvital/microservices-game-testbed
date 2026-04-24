using OnlineGame.InventoryService.Core.Domain.Entities;
using OnlineGame.InventoryService.Core.Application.Repositories;
using OnlineGame.InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace OnlineGame.InventoryService.Infrastructure.Repositories;

public class PlayerDeckRepository : IPlayerDeckRepository
{
    private readonly InventoryDbContext _db;
    public PlayerDeckRepository(InventoryDbContext db) => _db = db;

    public async Task<PlayerDeck?> GetByIdAsync(Guid deckId, CancellationToken cancellationToken = default)
        => await _db.PlayerDecks
            .Include(d => d.Slots)
            .FirstOrDefaultAsync(d => d.Id == deckId, cancellationToken);

    public async Task<List<PlayerDeck>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default)
        => await _db.PlayerDecks
            .Include(d => d.Slots)
            .Where(d => d.PlayerId == playerId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PlayerDeck deck, CancellationToken cancellationToken = default)
    {
        _db.PlayerDecks.Add(deck);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlayerDeck deck, CancellationToken cancellationToken = default)
    {
        _db.PlayerDecks.Update(deck);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        var deck = await _db.PlayerDecks.FindAsync(new object[] { deckId }, cancellationToken);
        if (deck != null)
        {
            _db.PlayerDecks.Remove(deck);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

