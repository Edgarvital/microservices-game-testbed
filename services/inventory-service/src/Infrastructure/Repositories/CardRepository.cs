using OnlineGame.InventoryService.Core.Domain.Entities;
using OnlineGame.InventoryService.Core.Application.Repositories;
using OnlineGame.InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace OnlineGame.InventoryService.Infrastructure.Repositories;

public class CardRepository : ICardRepository
{
    private readonly InventoryDbContext _db;
    public CardRepository(InventoryDbContext db) => _db = db;

    public async Task<Card?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Cards.FindAsync(new object[] { id }, cancellationToken);

    public async Task<List<Card>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Cards.ToListAsync(cancellationToken);

    public async Task AddAsync(Card card, CancellationToken cancellationToken = default)
    {
        _db.Cards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Card card, CancellationToken cancellationToken = default)
    {
        _db.Cards.Update(card);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

