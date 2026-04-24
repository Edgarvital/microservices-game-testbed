using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OnlineGame.InventoryService.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task RunSeeder(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<InventoryDbContext>();

            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }

            await CardDataSeeder.SeedAsync(context);
            
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<InventoryDbContext>>();
            logger.LogError(ex, "Ocorreu um erro ao inicializar e popular o banco de dados do InventoryService.");
            throw; 
        }
    }
}