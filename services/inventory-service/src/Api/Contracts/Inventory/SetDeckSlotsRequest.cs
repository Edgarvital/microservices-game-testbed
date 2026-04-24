using System.ComponentModel.DataAnnotations;

namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class SetDeckSlotsRequest
{
    [Required]
    [MinLength(1)]
    public List<DeckSlotInput> Slots { get; init; } = new();
}

public sealed class DeckSlotInput
{
    [Range(1, int.MaxValue)]
    public int SlotIndex { get; init; }

    [Required]
    public Guid CardId { get; init; }
}
