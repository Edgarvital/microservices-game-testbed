using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OnlineGame.InventoryService.Infrastructure.Persistence;

public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=onlinegame_inventory;Username=postgres;Password=postgres");
        return new InventoryDbContext(optionsBuilder.Options);
    }
}

