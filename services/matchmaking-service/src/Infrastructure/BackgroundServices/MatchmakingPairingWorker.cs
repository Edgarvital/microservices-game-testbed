using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Queue;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;
using OnlineGame.MatchmakingService.Core.Application.Repositories;
using StackExchange.Redis;

namespace OnlineGame.MatchmakingService.Infrastructure.BackgroundServices;

public sealed class MatchmakingPairingWorker : BackgroundService
{
    private const string MatchFoundChannel = "battle:match-found";

    private readonly ILogger<MatchmakingPairingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;

    public MatchmakingPairingWorker(
        ILogger<MatchmakingPairingWorker> logger,
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _redis = redis;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var matchmakingQueue = scope.ServiceProvider.GetRequiredService<IMatchmakingQueue>();
            var arenaRepository = scope.ServiceProvider.GetRequiredService<IArenaRepository>();
            var matchmakingStatusStore = scope.ServiceProvider.GetRequiredService<IMatchmakingStatusStore>();

            var arenaIds = await matchmakingQueue.GetArenaIdsAsync(stoppingToken);

            foreach (var arenaId in arenaIds)
            {
                var queueLength = await matchmakingQueue.GetQueueLengthAsync(arenaId, stoppingToken);
                while (queueLength >= 2)
                {
                    var first = await matchmakingQueue.DequeueAsync(arenaId, stoppingToken);
                    var second = await matchmakingQueue.DequeueAsync(arenaId, stoppingToken);

                    if (first is null || second is null)
                    {
                        break;
                    }

                    var arena = await arenaRepository.GetByIdAsync(arenaId, stoppingToken);
                    var matchId = Guid.NewGuid();

                    await matchmakingStatusStore.SetMatchedAsync(
                        first.PlayerId,
                        second.PlayerId,
                        arenaId,
                        arena?.Name ?? "Unknown",
                        first.Power,
                        second.Power,
                        matchId,
                        stoppingToken);

                    var matchFoundEvent = new MatchFoundEvent(
                        matchId.ToString(),
                        arenaId.ToString(),
                        arena?.Name ?? "Unknown",
                        first.PlayerId.ToString(),
                        second.PlayerId.ToString(),
                        first.Power,
                        second.Power);

                    await _redis.GetSubscriber().PublishAsync(
                        RedisChannel.Literal(MatchFoundChannel),
                        JsonSerializer.Serialize(matchFoundEvent, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

                    _logger.LogInformation(
                        "Partida Encontrada | MatchId: {MatchId} | Arena: {ArenaName} ({ArenaId}) | Players: {PlayerA} vs {PlayerB} | Power: {PowerA} vs {PowerB}",
                        matchId,
                        arena?.Name ?? "Unknown",
                        arenaId,
                        first.PlayerId,
                        second.PlayerId,
                        first.Power,
                        second.Power);

                    queueLength -= 2;
                }
            }
        }
    }
}

internal sealed record MatchFoundEvent(
    string MatchId,
    string ArenaId,
    string ArenaName,
    string PlayerAId,
    string PlayerBId,
    int PlayerAPower,
    int PlayerBPower);
