using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Roomly.Api.Features.Auth;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // Identity tablo adını açıkça "AspNetUsers" koyar; adlandırma paketi açık adları çevirmez.
        builder.ToTable(AuthDbNames.UsersTable);

        builder.Property(u => u.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength).IsRequired();

        // Identity'nin açıkça adlandırdığı EmailIndex ve UserNameIndex snake_case'e çevrilmez;
        // burada yeniden adlandırılır, e-posta index'i benzersiz yapılır.
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName(AuthDbNames.UsersNormalizedEmailUnique)
            .IsUnique();
        builder.HasIndex(u => u.NormalizedUserName)
            .HasDatabaseName(AuthDbNames.UsersNormalizedUserNameUnique)
            .IsUnique();
    }
}
