using OnlineGame.InventoryService.Core.Domain.Entities;

namespace OnlineGame.InventoryService.Core.Application.Repositories;

public interface IPlayerDeckRepository
{
    Task<PlayerDeck?> GetByIdAsync(Guid deckId, CancellationToken cancellationToken = default);
    Task<List<PlayerDeck>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task AddAsync(PlayerDeck deck, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlayerDeck deck, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid deckId, CancellationToken cancellationToken = default);
}

