namespace OnlineGame.AuthService.Core.Domain;

public class Player
{
	public Guid Id { get; private set; }
	public string? DeviceId { get; private set; }
	public string? Email { get; private set; }
	public string? PasswordHash { get; private set; }
	public int BaseLevel { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime LastLoginAt { get; private set; }
	public bool IsGuest => string.IsNullOrWhiteSpace(Email);

	private Player()
	{
	}

	public static Player CreateGuest(string deviceId)
	{
		if (string.IsNullOrWhiteSpace(deviceId))
		{
			throw new ArgumentException("DeviceId is required for guest players.", nameof(deviceId));
		}

		return new Player
		{
			Id = Guid.NewGuid(),
			DeviceId = deviceId.Trim(),
			Email = null,
			PasswordHash = null,
			BaseLevel = 1,
			CreatedAt = DateTime.UtcNow,
			LastLoginAt = DateTime.UtcNow
		};
	}

	public static Player CreateRegistered(string email, string passwordHash)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			throw new ArgumentException("Email is required for registered players.", nameof(email));
		}

		if (string.IsNullOrWhiteSpace(passwordHash))
		{
			throw new ArgumentException("PasswordHash is required for registered players.", nameof(passwordHash));
		}

		return new Player
		{
			Id = Guid.NewGuid(),
			DeviceId = null,
			Email = email.Trim(),
			PasswordHash = passwordHash.Trim(),
			BaseLevel = 1,
			CreatedAt = DateTime.UtcNow,
			LastLoginAt = DateTime.UtcNow
		};
	}

	public static Player Rehydrate(
		Guid id,
		string? deviceId,
		string? email,
		string? passwordHash,
		int baseLevel,
		DateTime createdAt,
		DateTime lastLoginAt)
	{
		if (id == Guid.Empty)
		{
			throw new ArgumentException("Id is required.", nameof(id));
		}

		if (baseLevel < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(baseLevel), "Base level must be at least 1.");
		}

		return new Player
		{
			Id = id,
			DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim(),
			Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
			PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash.Trim(),
			BaseLevel = baseLevel,
			CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
			LastLoginAt = DateTime.SpecifyKind(lastLoginAt, DateTimeKind.Utc)
		};
	}

	public void LinkAccount(string email, string passwordHash)
	{
		if (!IsGuest)
		{
			throw new InvalidOperationException("Only guest players can link a registered account.");
		}

		if (string.IsNullOrWhiteSpace(email))
		{
			throw new ArgumentException("Email is required.", nameof(email));
		}

		if (string.IsNullOrWhiteSpace(passwordHash))
		{
			throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
		}

		Email = email.Trim();
		PasswordHash = passwordHash.Trim();
	}

	public void RecordLogin()
	{
		LastLoginAt = DateTime.UtcNow;
	}
}