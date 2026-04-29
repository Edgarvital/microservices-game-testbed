using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineGame.MatchmakingService.Infrastructure.Persistence;

namespace OnlineGame.MatchmakingService.Infrastructure;

public static class InitializationExtensions
{
    public static async Task InitializeInfrastructureAsync(this IServiceProvider serviceProvider)
    {
        await serviceProvider.RunSeeder();
    }
}
