using System.ComponentModel.DataAnnotations;

namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class UpdatePlayerPowerRequest
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Power must be a non-negative integer.")]
    public int Power { get; set; }
}
