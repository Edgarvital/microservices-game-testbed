using Microsoft.EntityFrameworkCore;
using OnlineGame.AuthService.Core.Application.Players;
using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Infrastructure.Persistence;

public sealed class PlayerRepository : IPlayerRepository
{
    private readonly AuthDbContext _dbContext;

    public PlayerRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Player?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        var normalizedDeviceId = deviceId.Trim();

        return await _dbContext.Players
            .FirstOrDefaultAsync(x => x.DeviceId == normalizedDeviceId, cancellationToken);
    }

    public async Task<Player?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalizedEmail = email.Trim();

        return await _dbContext.Players
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);

        await _dbContext.Players.AddAsync(player, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Player player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (_dbContext.Entry(player).State == EntityState.Detached)
        {
            _dbContext.Players.Update(player);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}