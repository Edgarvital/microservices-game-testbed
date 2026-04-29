using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Queue;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;
using OnlineGame.MatchmakingService.Core.Application.Repositories;

namespace OnlineGame.MatchmakingService.Infrastructure.BackgroundServices;

public sealed class MatchmakingPairingWorker : BackgroundService
{
    private readonly ILogger<MatchmakingPairingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public MatchmakingPairingWorker(
        ILogger<MatchmakingPairingWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
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
