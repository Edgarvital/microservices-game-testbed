using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Core.Application.Players;

public interface IPlayerRepository
{
    Task<Player?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<Player?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Player player, CancellationToken cancellationToken = default);
    Task UpdateAsync(Player player, CancellationToken cancellationToken = default);
}