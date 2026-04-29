using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OnlineGame.MatchmakingService.Infrastructure.Persistence;

public sealed class MatchmakingDbContextFactory : IDesignTimeDbContextFactory<MatchmakingDbContext>
{
    public MatchmakingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MatchmakingDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=onlinegame_matchmaking;Username=postgres;Password=postgres");
        return new MatchmakingDbContext(optionsBuilder.Options);
    }
}
