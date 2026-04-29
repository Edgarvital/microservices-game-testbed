namespace OnlineGame.MatchmakingService.Core.Domain.Entities;

public sealed class Arena
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int MinPower { get; private set; }
    public int? MaxPower { get; private set; }

    private Arena() { }

    public Arena(Guid id, string name, int minPower, int? maxPower)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Arena name is required.", nameof(name));
        }

        if (minPower < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minPower));
        }

        if (maxPower is not null && maxPower < minPower)
        {
            throw new ArgumentException("MaxPower must be greater than or equal to MinPower.", nameof(maxPower));
        }

        Id = id;
        Name = name.Trim();
        MinPower = minPower;
        MaxPower = maxPower;
    }

    public bool AcceptsPower(int power)
    {
        if (power < 0)
        {
            return false;
        }

        return power >= MinPower && (MaxPower is null || power <= MaxPower.Value);
    }
}
