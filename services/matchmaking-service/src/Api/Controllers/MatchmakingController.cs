using Microsoft.AspNetCore.Mvc;
using OnlineGame.MatchmakingService.Api.Contracts.Common;
using OnlineGame.MatchmakingService.Api.Contracts.Matchmaking;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;

namespace OnlineGame.MatchmakingService.Api.Controllers;

[ApiController]
[Route("api/matchmaking")]
public sealed class MatchmakingController : ControllerBase
{
    private readonly IJoinMatchmakingHandler _joinMatchmakingHandler;
    private readonly IMatchmakingStatusStore _matchmakingStatusStore;

    public MatchmakingController(IJoinMatchmakingHandler joinMatchmakingHandler, IMatchmakingStatusStore matchmakingStatusStore)
    {
        _joinMatchmakingHandler = joinMatchmakingHandler;
        _matchmakingStatusStore = matchmakingStatusStore;
    }

    [HttpPost("join")]
    [ProducesResponseType<JoinMatchmakingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JoinMatchmakingResponse>> Join([FromBody] JoinMatchmakingRequest request, CancellationToken cancellationToken)
    {
        var result = await _joinMatchmakingHandler.HandleAsync(new JoinMatchmakingCommand(request.PlayerId), cancellationToken);

        return Ok(new JoinMatchmakingResponse
        {
            PlayerId = result.PlayerId,
            Power = result.Power,
            ArenaId = result.ArenaId,
            ArenaName = result.ArenaName,
            QueueKey = result.QueueKey,
            QueuePosition = result.QueuePosition
        });
    }

    [HttpGet("status/{playerId:guid}")]
    [ProducesResponseType<MatchmakingStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchmakingStatusResponse>> GetStatus(Guid playerId, CancellationToken cancellationToken)
    {
        var status = await _matchmakingStatusStore.GetByPlayerIdAsync(playerId, cancellationToken);
        if (status is null)
        {
            return NotFound(new ApiErrorResponse
            {
                Code = "MATCHMAKING_STATUS_NOT_FOUND",
                Message = "No matchmaking status was found for the provided playerId."
            });
        }

        return Ok(new MatchmakingStatusResponse
        {
            PlayerId = status.PlayerId,
            Status = status.Status.ToString(),
            ArenaId = status.ArenaId,
            ArenaName = status.ArenaName,
            Power = status.Power,
            MatchId = status.MatchId,
            OpponentId = status.OpponentId,
            UpdatedAtUtc = status.UpdatedAtUtc
        });
    }
}
