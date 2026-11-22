using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roomly.Api.Features.Auth;

namespace Roomly.Api.Features.Groups;

internal sealed class InviteCodeConfiguration : IEntityTypeConfiguration<InviteCode>
{
    public void Configure(EntityTypeBuilder<InviteCode> builder)
    {
        builder.ToTable("invite_codes", t => t.HasCheckConstraint(
            GroupsDbNames.InviteCodesCodeHashLength,
            $"octet_length(code_hash) = {InviteCodeRules.HashLength}"));

        builder.HasKey(c => c.Id);

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(c => c.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.CodeHash, GroupsDbNames.InviteCodesCodeHashUnique)
            .HasDatabaseName(GroupsDbNames.InviteCodesCodeHashUnique).IsUnique();

        // Grupta tek iptal edilmemiş kod. Yeni kodda önce eskisi iptal edilmeli (yazma sırası).
        builder.HasIndex(c => c.GroupId, GroupsDbNames.InviteCodesOneActive)
            .HasDatabaseName(GroupsDbNames.InviteCodesOneActive)
            .IsUnique()
            .HasFilter("revoked_at IS NULL");
    }
}
