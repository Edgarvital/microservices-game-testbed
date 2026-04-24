using System.ComponentModel.DataAnnotations;
using OnlineGame.InventoryService.Core.Domain.Enums;

namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class CreateCardRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; init; } = string.Empty;

    [Range(1, 100000)]
    public int BaseDamage { get; init; }

    [Required]
    public CardRarity Rarity { get; init; }
}
