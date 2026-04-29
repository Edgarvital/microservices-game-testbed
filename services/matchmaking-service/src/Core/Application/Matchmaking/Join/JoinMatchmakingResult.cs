namespace OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;

public sealed record JoinMatchmakingResult(Guid PlayerId, Guid ArenaId, string ArenaName, int Power, string QueueKey, long QueuePosition);
