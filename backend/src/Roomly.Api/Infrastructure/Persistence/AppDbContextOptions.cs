using Microsoft.EntityFrameworkCore;

namespace Roomly.Api.Infrastructure.Persistence;

/// <summary>
/// <see cref="AppDbContext"/> için tek veritabanı ayarı: Npgsql, snake_case adlar ve
/// <c>__ef_migrations_history</c>. Uygulama ve entegrasyon testleri aynı ayarı kullanır.
/// </summary>
public static class AppDbContextOptions
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public static DbContextOptionsBuilder UseRoomlyNpgsql(
        this DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention();
}
