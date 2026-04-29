namespace OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;

public sealed record MatchmakingTicket(Guid PlayerId, int Power, DateTime JoinedAtUtc);