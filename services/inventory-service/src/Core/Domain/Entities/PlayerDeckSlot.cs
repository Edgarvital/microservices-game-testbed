namespace OnlineGame.InventoryService.Core.Domain.Entities;

public class PlayerDeckSlot
{
    public Guid DeckId { get; private set; }
    public int SlotIndex { get; private set; }
    public Guid CardId { get; private set; }

    // Navigation
    public PlayerDeck Deck { get; private set; } = null!;
    public Card Card { get; private set; } = null!;

    private PlayerDeckSlot() { }

    public PlayerDeckSlot(Guid deckId, int slotIndex, Guid cardId)
    {
        DeckId = deckId;
        SlotIndex = slotIndex;
        CardId = cardId;
    }
}

