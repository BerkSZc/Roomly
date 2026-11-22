using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Roomly.Api.Features.Auth;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", t => t.HasCheckConstraint(
            AuthDbNames.RefreshTokensTokenHashLength,
            $"octet_length(token_hash) = {RefreshTokenRules.HashLength}"));

        builder.HasKey(t => t.Id);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(t => t.ReplacedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.TokenHash, AuthDbNames.RefreshTokensTokenHashUnique)
            .HasDatabaseName(AuthDbNames.RefreshTokensTokenHashUnique).IsUnique();
        builder.HasIndex(t => t.ReplacedById, AuthDbNames.RefreshTokensReplacedByIdUnique)
            .HasDatabaseName(AuthDbNames.RefreshTokensReplacedByIdUnique).IsUnique();
        builder.HasIndex(t => t.UserId, AuthDbNames.RefreshTokensUserIdIndex)
            .HasDatabaseName(AuthDbNames.RefreshTokensUserIdIndex);
        builder.HasIndex(t => t.FamilyId, AuthDbNames.RefreshTokensFamilyIdIndex)
            .HasDatabaseName(AuthDbNames.RefreshTokensFamilyIdIndex);
    }
}
