using System.ComponentModel.DataAnnotations;

namespace OnlineGame.InventoryService.Api.Contracts.Inventory;

public sealed class EvolveCardRequest
{
    [Range(1, 1000)]
    public int DuplicatesRequired { get; init; }
}
