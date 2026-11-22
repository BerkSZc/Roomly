using Microsoft.EntityFrameworkCore;

namespace Roomly.Api.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın tek DbContext'i. Varlıklar ve ilk migration "Data model" özelliğiyle gelir.
/// Ortak dosyadır: değişiklikler onaya bağlıdır.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
