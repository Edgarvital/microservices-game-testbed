using System.ComponentModel.DataAnnotations;

namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class GrantCardRewardRequest
{
    [Range(1, 1000)]
    public int Amount { get; init; } = 1;
}
