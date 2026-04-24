using OnlineGame.InventoryService.Core.Domain.Enums;

namespace OnlineGame.InventoryService.Core.Domain.Entities;

public class Card
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int BaseDamage { get; private set; }
    public CardRarity Rarity { get; private set; }

    private Card() { }

    public Card(Guid id, string name, string description, int baseDamage, CardRarity rarity)
    {
        Id = id;
        Name = name;
        Description = description;
        BaseDamage = baseDamage;
        Rarity = rarity;
    }
}

