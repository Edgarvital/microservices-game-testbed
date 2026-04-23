namespace OnlineGame.AuthService.Core.Application.Security;

public interface ISecretProvider
{
    Task<string> GetJwtSecretAsync();
}