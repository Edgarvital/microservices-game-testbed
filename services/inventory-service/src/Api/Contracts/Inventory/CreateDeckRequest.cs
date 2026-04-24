using System.ComponentModel.DataAnnotations;

namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class CreateDeckRequest
{
    [Required]
    [MaxLength(60)]
    public string Name { get; init; } = string.Empty;
}
