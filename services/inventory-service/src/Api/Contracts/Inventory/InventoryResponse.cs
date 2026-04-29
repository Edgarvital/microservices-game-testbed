namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class InventoryResponse
{
    public Guid PlayerId { get; init; }
    public int BaseLevel { get; init; }
    public int BaseHp { get; init; }
    public int BaseAttack { get; init; }
    public int Power { get; init; }
    public DateTime LastUpdate { get; init; }
    public IReadOnlyList<PlayerCardResponse> Cards { get; init; } = Array.Empty<PlayerCardResponse>();
    public IReadOnlyList<PlayerDeckResponse> Decks { get; init; } = Array.Empty<PlayerDeckResponse>();
}

public sealed class PlayerCardResponse
{
    public Guid CardId { get; init; }
    public string CardName { get; init; } = string.Empty;
    public int CurrentLevel { get; init; }
    public int CountDuplicates { get; init; }
    public DateTime DateObtained { get; init; }
    public DateTime? LastUpgrade { get; init; }
}

public sealed class PlayerDeckResponse
{
    public Guid DeckId { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<DeckSlotResponse> Slots { get; init; } = Array.Empty<DeckSlotResponse>();
}

public sealed class DeckSlotResponse
{
    public int SlotIndex { get; init; }
    public Guid CardId { get; init; }
}
