using Api.Application.Users;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Infrastructure;

public static class DependencyInjection
{
    /// <summary>PostgreSQL (EF Core), the repositories and the database health check.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Default (see .env.example).");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services;
    }

    /// <summary>
    /// Brings the database up to date, then, when <paramref name="seedDemoData"/> is set, adds the
    /// demo users to an empty table. Returns how many users were added.
    /// </summary>
    public static async Task<int> PrepareDatabaseAsync(
        this IServiceProvider services, bool seedDemoData, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
        return seedDemoData
            ? await DemoData.SeedAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>(), ct)
            : 0;
    }
}
