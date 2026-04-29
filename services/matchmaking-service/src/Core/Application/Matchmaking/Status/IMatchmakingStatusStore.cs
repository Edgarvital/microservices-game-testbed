namespace OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;

public interface IMatchmakingStatusStore
{
    Task SetQueuedAsync(Guid playerId, Guid arenaId, string arenaName, int power, CancellationToken cancellationToken = default);
    Task SetMatchedAsync(Guid firstPlayerId, Guid secondPlayerId, Guid arenaId, string arenaName, int firstPower, int secondPower, Guid matchId, CancellationToken cancellationToken = default);
    Task<MatchmakingPlayerStatus?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);
}
