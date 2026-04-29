namespace OnlineGame.MatchmakingService.Core.Application.Power;

public interface IPowerProvider
{
    Task<int> GetPowerAsync(Guid playerId, CancellationToken cancellationToken = default);
}
