using Microsoft.Extensions.Configuration;
using OnlineGame.AuthService.Core.Application.Security;

namespace OnlineGame.AuthService.Infrastructure.Security;

public sealed class LocalSecretProvider : ISecretProvider
{
    private readonly IConfiguration _configuration;

    public LocalSecretProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<string> GetJwtSecretAsync()
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET");

        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = _configuration["Jwt:Secret"];
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret was not configured. Set JWT_SECRET or Jwt:Secret.");
        }

        return Task.FromResult(secret.Trim());
    }
}