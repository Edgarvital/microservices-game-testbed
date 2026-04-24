using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineGame.InventoryService.Core.Application.Repositories;
using OnlineGame.InventoryService.Infrastructure.Persistence;
using OnlineGame.InventoryService.Infrastructure.Repositories;

namespace OnlineGame.InventoryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb")
            ?? throw new InvalidOperationException("Connection string 'InventoryDb' was not found.");

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<IPlayerInventoryRepository, PlayerInventoryRepository>();
        services.AddScoped<IPlayerDeckRepository, PlayerDeckRepository>();

        return services;
    }
}
