using OnlineGame.InventoryService.Core.Domain.Enums;

namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class CardResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int BaseDamage { get; init; }
    public CardRarity Rarity { get; init; }
}
