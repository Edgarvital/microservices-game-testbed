namespace OnlineGame.InventoryService.Core.Domain.Entities;

public class PlayerCard
{
    public Guid PlayerId { get; private set; }
    public Guid CardId { get; private set; }
    public int CurrentLevel { get; private set; }
    public int CountDuplicates { get; private set; }
    public DateTime DateObtained { get; private set; }
    public DateTime? LastUpgrade { get; private set; }

    // Navigation
    public Card Card { get; private set; } = null!;

    private PlayerCard() { }

    public PlayerCard(Guid playerId, Guid cardId, int currentLevel, int countDuplicates, DateTime dateObtained, DateTime? lastUpgrade)
    {
        PlayerId = playerId;
        CardId = cardId;
        CurrentLevel = currentLevel;
        CountDuplicates = countDuplicates;
        DateObtained = dateObtained;
        LastUpgrade = lastUpgrade;
    }

    public void AddDuplicates(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Duplicate amount must be greater than zero.");
        }

        CountDuplicates += amount;
    }

    public bool CanEvolve(int duplicatesRequired)
    {
        return duplicatesRequired > 0 && CountDuplicates >= duplicatesRequired;
    }

    public void EvolveUsingDuplicates(int duplicatesRequired, DateTime upgradeDate)
    {
        if (!CanEvolve(duplicatesRequired))
        {
            throw new InvalidOperationException("Not enough duplicates to evolve this card.");
        }

        CurrentLevel += 1;
        CountDuplicates -= duplicatesRequired;
        LastUpgrade = upgradeDate;
    }

    public void Evolve(int newLevel, int newDuplicates, DateTime upgradeDate)
    {
        CurrentLevel = newLevel;
        CountDuplicates = newDuplicates;
        LastUpgrade = upgradeDate;
    }
}

