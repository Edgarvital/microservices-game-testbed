using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineGame.AuthService.Core.Application.Players;
using OnlineGame.AuthService.Core.Application.Security;
using OnlineGame.AuthService.Infrastructure.Persistence;
using OnlineGame.AuthService.Infrastructure.Security;

namespace OnlineGame.AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("AUTH_DB_CONNECTION")
            ?? configuration.GetConnectionString("AuthDb")
            ?? throw new InvalidOperationException("Auth DB connection string is missing. Configure AUTH_DB_CONNECTION or ConnectionStrings:AuthDb.");

        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

        var provider = configuration["Secrets:Provider"];

        if (string.Equals(provider, "Vault", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<VaultSecretProvider>();
            services.AddSingleton<ISecretProvider>(sp => sp.GetRequiredService<VaultSecretProvider>());
        }
        else
        {
            services.AddSingleton<ISecretProvider>(new LocalSecretProvider(configuration));
        }

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

    public static async Task InitializeInfrastructureAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}