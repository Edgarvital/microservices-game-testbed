using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineGame.AuthService.Core.Application.Players;
using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _dbContext;

    public UserRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        var normalizedDeviceId = deviceId.Trim();

        return await _dbContext.Users
            .FirstOrDefaultAsync(x => x.DeviceId == normalizedDeviceId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalizedEmail = email.Trim();

        return await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (_dbContext.Entry(user).State == EntityState.Detached)
        {
            _dbContext.Users.Update(user);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}