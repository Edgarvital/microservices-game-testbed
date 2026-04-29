using System.Text.Json;
using StackExchange.Redis;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;

namespace OnlineGame.MatchmakingService.Infrastructure.Redis;

public sealed class RedisMatchmakingStatusStore : IMatchmakingStatusStore
{
    private const string StatusPrefix = "matchmaking:player:";
    private static readonly TimeSpan StatusTtl = TimeSpan.FromMinutes(30);
    private readonly IDatabase _database;

    public RedisMatchmakingStatusStore(IConnectionMultiplexer multiplexer)
    {
        _database = multiplexer.GetDatabase();
    }

    public async Task SetQueuedAsync(Guid playerId, Guid arenaId, string arenaName, int power, CancellationToken cancellationToken = default)
    {
        var status = new MatchmakingPlayerStatus(
            playerId,
            MatchmakingStatus.Queued,
            arenaId,
            arenaName,
            power,
            null,
            null,
            DateTime.UtcNow);

        await SetAsync(playerId, status, cancellationToken);
    }

    public async Task SetMatchedAsync(Guid firstPlayerId, Guid secondPlayerId, Guid arenaId, string arenaName, int firstPower, int secondPower, Guid matchId, CancellationToken cancellationToken = default)
    {
        var firstStatus = new MatchmakingPlayerStatus(
            firstPlayerId,
            MatchmakingStatus.Matched,
            arenaId,
            arenaName,
            firstPower,
            matchId,
            secondPlayerId,
            DateTime.UtcNow);

        var secondStatus = new MatchmakingPlayerStatus(
            secondPlayerId,
            MatchmakingStatus.Matched,
            arenaId,
            arenaName,
            secondPower,
            matchId,
            firstPlayerId,
            DateTime.UtcNow);

        await SetAsync(firstPlayerId, firstStatus, cancellationToken);
        await SetAsync(secondPlayerId, secondStatus, cancellationToken);
    }

    public async Task<MatchmakingPlayerStatus?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(GetKey(playerId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<MatchmakingPlayerStatus>(value!);
    }

    private Task SetAsync(Guid playerId, MatchmakingPlayerStatus status, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(status);
        return _database.StringSetAsync(GetKey(playerId), payload, StatusTtl);
    }

    private static RedisKey GetKey(Guid playerId) => $"{StatusPrefix}{playerId:N}";
}
