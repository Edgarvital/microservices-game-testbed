namespace OnlineGame.AuthService.Api.Contracts.Common;

public sealed class ApiErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IDictionary<string, string[]>? Details { get; init; }
}