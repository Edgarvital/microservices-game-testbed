using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Core.Application.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(Player player, string secretKey);
}