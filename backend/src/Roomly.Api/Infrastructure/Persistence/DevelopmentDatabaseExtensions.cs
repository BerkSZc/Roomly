using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Roomly.Api.Infrastructure.Persistence;

public static class DevelopmentDatabaseExtensions
{
    private const int MigrateAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public static IServiceCollection AddDevDataSeeding(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DevSeedOptions>(configuration.GetSection(DevSeedOptions.SectionName));
        services.AddScoped<DevDataSeeder>();
        return services;
    }

    /// <summary>
    /// Yalnızca Development: migration'ları uygular (veritabanı hazır değilse 5 deneme, 2 sn arayla) ve demo
    /// veriyi yazar (spec 0001).
    /// </summary>
    public static async Task InitializeDevelopmentDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<DevDataSeeder>>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await services.GetRequiredService<AppDbContext>().Database.MigrateAsync();
                break;
            }
            catch (NpgsqlException ex) when (attempt < MigrateAttempts)
            {
                logger.LogWarning(ex, "Veritabanına ulaşılamadı ({Attempt}/{Max}), tekrar denenecek.", attempt, MigrateAttempts);
                await Task.Delay(RetryDelay);
            }
        }

        await services.GetRequiredService<DevDataSeeder>().SeedAsync();
    }
}
