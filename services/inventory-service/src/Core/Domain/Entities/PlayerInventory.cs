namespace OnlineGame.InventoryService.Core.Domain.Entities;

public class PlayerInventory
{
    public Guid PlayerId { get; private set; }
    public int BaseLevel { get; private set; }
    public int BaseHp { get; private set; }
    public int BaseAttack { get; private set; }
    public int Power { get; private set; }
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
        Power = baseAttack; // Inicializa com BaseAttack
        LastUpdate = lastUpdate;
    }

    public void Touch(DateTime timestamp)
    {
        LastUpdate = timestamp;
    }

    /// <summary>
    /// Atualiza o poder do jogador. Será calculado baseado em cartas evoluídas e combats futuros.
    /// </summary>
    public void SetPower(int newPower)
    {
        if (newPower < 0)
        {
            throw new ArgumentException("Power cannot be negative.", nameof(newPower));
        }

        Power = newPower;
        LastUpdate = DateTime.UtcNow;
    }
}

