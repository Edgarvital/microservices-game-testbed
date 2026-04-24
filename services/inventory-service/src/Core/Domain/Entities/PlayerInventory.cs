namespace OnlineGame.InventoryService.Core.Domain.Entities;

public class PlayerInventory
{
    public Guid PlayerId { get; private set; }
    public int BaseLevel { get; private set; }
    public int BaseHp { get; private set; }
    public int BaseAttack { get; private set; }
    public DateTime LastUpdate { get; private set; }

    // Navigation
    public List<PlayerCard> Cards { get; private set; } = new();
    public List<PlayerDeck> Decks { get; private set; } = new();

    private PlayerInventory() { }

    public PlayerInventory(Guid playerId, int baseLevel, int baseHp, int baseAttack, DateTime lastUpdate)
    {
        PlayerId = playerId;
        BaseLevel = baseLevel;
        BaseHp = baseHp;
        BaseAttack = baseAttack;
        LastUpdate = lastUpdate;
    }

    public void Touch(DateTime timestamp)
    {
        LastUpdate = timestamp;
    }
}

