using Microsoft.EntityFrameworkCore;
using OnlineGame.InventoryService.Core.Domain.Entities;
using OnlineGame.InventoryService.Core.Domain.Enums;

namespace OnlineGame.InventoryService.Infrastructure.Persistence;

public static class CardDataSeeder
{
    public static async Task SeedAsync(InventoryDbContext context)
    {
        // Verifica se já existem cartas para não duplicar os dados
        if (await context.Cards.AnyAsync())
        {
            return;
        }

        var cards = new List<Card>
        {
            // Instanciando através do construtor público
            new Card(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Bola de Fogo",
                "Bola de Fogo é um feitiço ofensivo que causa dano em área, atingindo múltiplos inimigos próximos ao alvo.",
                80,
                CardRarity.Common
            ),
            new Card(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Geada",
                "Geada é um feitiço de defesa que congela inimigos em uma área, impedindo-os de se mover.",
                20,
                CardRarity.Rare
            ),
            new Card(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Tempestade de Raios",
                "Tempestade de Raios é um feitiço de ataque que causa dano em área com raios.",
                120,
                CardRarity.Epic
            )
        };

        await context.Cards.AddRangeAsync(cards);
        await context.SaveChangesAsync();
    }
}