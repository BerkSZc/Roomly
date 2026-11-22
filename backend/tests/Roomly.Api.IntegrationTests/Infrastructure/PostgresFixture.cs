using Microsoft.EntityFrameworkCore;
using Roomly.Api.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Roomly.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Tüm entegrasyon testlerinin paylaştığı tek PostgreSQL 16 konteyneri. Açılışta migration'lar
/// uygulanır. Testler birbirinin verisine dokunmaz: her test kendi kullanıcı ve grubunu benzersiz
/// kimliklerle kurar. İleride <c>WebApplicationFactory&lt;Program&gt;</c> da aynı konteyneri kullanır.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Uygulamayla aynı ayarlarla (snake_case, migration geçmişi tablosu) yeni bir context.</summary>
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseRoomlyNpgsql(ConnectionString);
        return new AppDbContext(options.Options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
