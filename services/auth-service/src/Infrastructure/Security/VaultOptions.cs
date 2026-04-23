using Microsoft.Extensions.Configuration;

namespace OnlineGame.AuthService.Infrastructure.Security;

public sealed class VaultOptions
{
    public string Address { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string MountPath { get; init; } = "secret";
    public string SecretPath { get; init; } = "onlinegame/auth";
    public string JwtSecretField { get; init; } = "jwtSecret";

    public static VaultOptions FromConfiguration(IConfiguration configuration)
    {
        return new VaultOptions
        {
            Address = configuration["Vault:Address"] ?? string.Empty,
            Token = configuration["Vault:Token"] ?? string.Empty,
            MountPath = configuration["Vault:MountPath"] ?? "secret",
            SecretPath = configuration["Vault:SecretPath"] ?? "onlinegame/auth",
            JwtSecretField = configuration["Vault:JwtSecretField"] ?? "jwtSecret"
        };
    }
}