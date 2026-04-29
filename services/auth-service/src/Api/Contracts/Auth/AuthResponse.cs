namespace OnlineGame.AuthService.Api.Contracts.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public Guid PlayerId { get; init; }
    public bool IsGuest { get; init; }
    public string? Email { get; init; }
    public string? DeviceId { get; init; }
}