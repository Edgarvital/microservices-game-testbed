namespace OnlineGame.MatchmakingService.Api.Contracts.Matchmaking;

public sealed class MatchmakingStatusResponse
{
    public Guid PlayerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid ArenaId { get; init; }
    public string ArenaName { get; init; } = string.Empty;
    public int Power { get; init; }
    public Guid? MatchId { get; init; }
    public Guid? OpponentId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
