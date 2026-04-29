using OnlineGame.MatchmakingService.Core.Domain.Entities;

namespace OnlineGame.MatchmakingService.Core.Application.Repositories;

public interface IArenaRepository
{
    Task<List<Arena>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Arena?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Arena?> FindByPowerAsync(int power, CancellationToken cancellationToken = default);
    Task AddAsync(Arena arena, CancellationToken cancellationToken = default);
}
