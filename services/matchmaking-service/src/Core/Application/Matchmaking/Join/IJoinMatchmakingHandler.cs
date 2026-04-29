namespace OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;

public interface IJoinMatchmakingHandler
{
    Task<JoinMatchmakingResult> HandleAsync(JoinMatchmakingCommand command, CancellationToken cancellationToken = default);
}
