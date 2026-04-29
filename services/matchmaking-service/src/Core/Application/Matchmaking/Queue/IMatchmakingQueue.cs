using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;

namespace OnlineGame.MatchmakingService.Core.Application.Matchmaking.Queue;

public interface IMatchmakingQueue
{
    Task<long> EnqueueAsync(Guid arenaId, MatchmakingTicket ticket, CancellationToken cancellationToken = default);
    Task<long> GetQueueLengthAsync(Guid arenaId, CancellationToken cancellationToken = default);
    Task<MatchmakingTicket?> DequeueAsync(Guid arenaId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetArenaIdsAsync(CancellationToken cancellationToken = default);
}