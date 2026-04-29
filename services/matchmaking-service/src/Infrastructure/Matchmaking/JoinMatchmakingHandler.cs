using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Queue;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;
using OnlineGame.MatchmakingService.Core.Application.Power;
using OnlineGame.MatchmakingService.Core.Application.Repositories;

namespace OnlineGame.MatchmakingService.Infrastructure.Matchmaking.Join;

public sealed class JoinMatchmakingHandler : IJoinMatchmakingHandler
{
    private readonly IPowerProvider _powerProvider;
    private readonly IArenaRepository _arenaRepository;
    private readonly IMatchmakingQueue _matchmakingQueue;
    private readonly IMatchmakingStatusStore _matchmakingStatusStore;

    public JoinMatchmakingHandler(
        IPowerProvider powerProvider,
        IArenaRepository arenaRepository,
        IMatchmakingQueue matchmakingQueue,
        IMatchmakingStatusStore matchmakingStatusStore)
    {
        _powerProvider = powerProvider;
        _arenaRepository = arenaRepository;
        _matchmakingQueue = matchmakingQueue;
        _matchmakingStatusStore = matchmakingStatusStore;
    }

    public async Task<JoinMatchmakingResult> HandleAsync(JoinMatchmakingCommand command, CancellationToken cancellationToken = default)
    {
        var power = await _powerProvider.GetPowerAsync(command.PlayerId, cancellationToken);
        var arena = await _arenaRepository.FindByPowerAsync(power, cancellationToken)
            ?? throw new InvalidOperationException($"No arena configured for power {power}.");

        var ticket = new MatchmakingTicket(command.PlayerId, power, DateTime.UtcNow);
        var queuePosition = await _matchmakingQueue.EnqueueAsync(arena.Id, ticket, cancellationToken);
        await _matchmakingStatusStore.SetQueuedAsync(command.PlayerId, arena.Id, arena.Name, power, cancellationToken);

        return new JoinMatchmakingResult(command.PlayerId, arena.Id, arena.Name, power, $"matchmaking:arena:{arena.Id:N}", queuePosition);
    }
}
