using Microsoft.EntityFrameworkCore;
using OnlineGame.MatchmakingService.Core.Application.Repositories;
using OnlineGame.MatchmakingService.Core.Domain.Entities;
using OnlineGame.MatchmakingService.Infrastructure.Persistence;

namespace OnlineGame.MatchmakingService.Infrastructure.Repositories;

public sealed class ArenaRepository : IArenaRepository
{
    private readonly MatchmakingDbContext _dbContext;

    public ArenaRepository(MatchmakingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Arena>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Arenas.OrderBy(x => x.MinPower).ToListAsync(cancellationToken);

    public async Task<Arena?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Arenas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Arena?> FindByPowerAsync(int power, CancellationToken cancellationToken = default)
        => await _dbContext.Arenas
            .OrderBy(x => x.MinPower)
            .FirstOrDefaultAsync(x => x.MinPower <= power && (x.MaxPower == null || power <= x.MaxPower), cancellationToken);

    public async Task AddAsync(Arena arena, CancellationToken cancellationToken = default)
    {
        _dbContext.Arenas.Add(arena);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
