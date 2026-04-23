using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OnlineGame.AuthService.Core.Application.Security;
using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly int _accessTokenTtlMinutes;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        var ttlValue = configuration["Jwt:AccessTokenTtlMinutes"];
        _accessTokenTtlMinutes = int.TryParse(ttlValue, out var parsed) && parsed > 0 ? parsed : 120;
    }

    public string GenerateToken(Player player, string secretKey)
    {
        if (player is null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new ArgumentException("Secret key is required.", nameof(secretKey));
        }

        var secretBytes = Encoding.UTF8.GetBytes(secretKey);
        if (secretBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 bytes for HS256.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, player.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("is_guest", player.IsGuest.ToString().ToLowerInvariant()),
            new("base_level", player.BaseLevel.ToString())
        };

        if (!string.IsNullOrWhiteSpace(player.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, player.Email));
        }

        if (!string.IsNullOrWhiteSpace(player.DeviceId))
        {
            claims.Add(new Claim("device_id", player.DeviceId));
        }

        var key = new SymmetricSecurityKey(secretBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenTtlMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}