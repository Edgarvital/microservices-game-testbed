using OnlineGame.InventoryService.Core.Domain.Entities;

namespace OnlineGame.InventoryService.Core.Application.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Card>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Card card, CancellationToken cancellationToken = default);
    Task UpdateAsync(Card card, CancellationToken cancellationToken = default);
}

