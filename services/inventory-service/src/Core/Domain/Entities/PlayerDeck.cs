namespace OnlineGame.InventoryService.Core.Domain.Entities;

public class PlayerDeck
{
    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Navigation
    public List<PlayerDeckSlot> Slots { get; private set; } = new();

    private PlayerDeck() { }

    public PlayerDeck(Guid id, Guid playerId, string name)
    {
        Id = id;
        PlayerId = playerId;
        Name = name;
    }
}

