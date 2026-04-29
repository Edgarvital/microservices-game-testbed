using System.Text.Json;
using StackExchange.Redis;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Queue;

namespace OnlineGame.MatchmakingService.Infrastructure.Redis;

public sealed class RedisMatchmakingQueue : IMatchmakingQueue
{
    private const string QueuePrefix = "matchmaking:arena:";
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IDatabase _database;

    public RedisMatchmakingQueue(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
        _database = _multiplexer.GetDatabase();
    }

    public async Task<long> EnqueueAsync(Guid arenaId, MatchmakingTicket ticket, CancellationToken cancellationToken = default)
    {
        var key = GetQueueKey(arenaId);
        var payload = JsonSerializer.Serialize(ticket);
        return await _database.ListRightPushAsync(key, payload);
    }

    public async Task<long> GetQueueLengthAsync(Guid arenaId, CancellationToken cancellationToken = default)
    {
        var key = GetQueueKey(arenaId);
        return await _database.ListLengthAsync(key);
    }

    public async Task<MatchmakingTicket?> DequeueAsync(Guid arenaId, CancellationToken cancellationToken = default)
    {
        var key = GetQueueKey(arenaId);
        var value = await _database.ListLeftPopAsync(key);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<MatchmakingTicket>(value!);
    }

    public Task<List<Guid>> GetArenaIdsAsync(CancellationToken cancellationToken = default)
    {
        var keys = new List<Guid>();

        foreach (var endpoint in _multiplexer.GetEndPoints())
        {
            var server = _multiplexer.GetServer(endpoint);
            foreach (var redisKey in server.Keys(pattern: $"{QueuePrefix}*"))
            {
                var keyValue = redisKey.ToString();
                var suffix = keyValue[QueuePrefix.Length..];
                if (Guid.TryParse(suffix, out var arenaId))
                {
                    keys.Add(arenaId);
                }
            }
        }

        return Task.FromResult(keys.Distinct().ToList());
    }

    private static RedisKey GetQueueKey(Guid arenaId) => $"{QueuePrefix}{arenaId:N}";
}
