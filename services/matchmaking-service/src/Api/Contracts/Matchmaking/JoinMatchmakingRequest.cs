using System.ComponentModel.DataAnnotations;

namespace OnlineGame.MatchmakingService.Api.Contracts.Matchmaking;

public sealed class JoinMatchmakingRequest
{
    [Required]
    public Guid PlayerId { get; init; }
}
