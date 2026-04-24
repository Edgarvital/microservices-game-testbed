using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineGame.InventoryService.Infrastructure.Persistence;

namespace OnlineGame.InventoryService.Infrastructure;

public static class InitializationExtensions
{
    public static async Task InitializeInfrastructureAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
