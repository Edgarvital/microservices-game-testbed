namespace OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;

public sealed record MatchmakingPlayerStatus(
    Guid PlayerId,
    MatchmakingStatus Status,
    Guid ArenaId,
    string ArenaName,
    int Power,
    Guid? MatchId,
    Guid? OpponentId,
    DateTime UpdatedAtUtc);
