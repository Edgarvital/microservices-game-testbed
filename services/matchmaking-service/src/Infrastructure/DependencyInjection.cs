using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Join;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Queue;
using OnlineGame.MatchmakingService.Core.Application.Matchmaking.Status;
using OnlineGame.MatchmakingService.Core.Application.Power;
using OnlineGame.MatchmakingService.Core.Application.Repositories;
using OnlineGame.MatchmakingService.Infrastructure.BackgroundServices;
using OnlineGame.MatchmakingService.Infrastructure.ExternalServices;
using OnlineGame.MatchmakingService.Infrastructure.Matchmaking.Join;
using OnlineGame.MatchmakingService.Infrastructure.Persistence;
using OnlineGame.MatchmakingService.Infrastructure.Repositories;
using OnlineGame.MatchmakingService.Infrastructure.Power;
using OnlineGame.MatchmakingService.Infrastructure.Redis;

namespace OnlineGame.MatchmakingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MatchmakingDb")
            ?? throw new InvalidOperationException("Connection string 'MatchmakingDb' was not found.");

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Connection string 'Redis' was not found.");

        services.AddDbContext<MatchmakingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

        // Cliente HTTP para chamar inventory-service
        services.AddHttpClient<InventoryServiceClient>((sp, client) =>
        {
            var inventoryServiceUrl = configuration["Services:InventoryServiceUrl"] 
                ?? "http://localhost:5001";
            client.BaseAddress = new Uri(inventoryServiceUrl);
        });

        services.AddScoped<IArenaRepository, ArenaRepository>();
        services.AddScoped<IMatchmakingQueue, RedisMatchmakingQueue>();
        services.AddScoped<IMatchmakingStatusStore, RedisMatchmakingStatusStore>();
        services.AddScoped<IPowerProvider, MockPowerProvider>();
        services.AddScoped<IJoinMatchmakingHandler, JoinMatchmakingHandler>();
        services.AddHostedService<MatchmakingPairingWorker>();

        return services;
    }
}
