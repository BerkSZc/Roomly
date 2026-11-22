using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth;

namespace Roomly.Api.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın tek DbContext'i. Identity yalnızca kullanıcı tablolarıyla gelir (rol tablosu yok).
/// Modüller <c>DbSet</c> eklemez: her entity'nin <c>IEntityTypeConfiguration</c> sınıfı kendi
/// modül klasöründe durur, burada otomatik bulunur; erişim <c>Set&lt;T&gt;()</c> ile yapılır.
/// Ortak dosyadır: değişiklikler onaya bağlıdır.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Önce Identity varsayılanları, sonra modül configuration'ları (Identity index adlarını ezer).
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
