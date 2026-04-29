using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineGame.InventoryService.Infrastructure.Persistence;

namespace OnlineGame.MatchmakingService.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task RunSeeder(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<MatchmakingDbContext>();

            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }

            await ArenaSeeder.SeedAsync(context);
            
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<MatchmakingDbContext>>();
            logger.LogError(ex, "Ocorreu um erro ao inicializar e popular o banco de dados do MatchmakingService.");
            throw; 
        }
    }
}