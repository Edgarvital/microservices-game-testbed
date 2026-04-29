using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Core.Application.Players;

public interface IUserRepository
{
    Task<User?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}