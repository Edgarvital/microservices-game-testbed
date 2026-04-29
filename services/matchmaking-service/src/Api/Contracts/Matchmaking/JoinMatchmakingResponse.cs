namespace OnlineGame.MatchmakingService.Api.Contracts.Matchmaking;

public sealed class JoinMatchmakingResponse
{
    public Guid PlayerId { get; init; }
    public int Power { get; init; }
    public Guid ArenaId { get; init; }
    public string ArenaName { get; init; } = string.Empty;
    public string QueueKey { get; init; } = string.Empty;
    public long QueuePosition { get; init; }
}
