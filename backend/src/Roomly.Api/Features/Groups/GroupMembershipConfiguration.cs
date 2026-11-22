using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Roomly.Api.Features.Auth;

namespace Roomly.Api.Features.Groups;

internal sealed class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> builder)
    {
        var roles = string.Join(", ", Enum.GetNames<MembershipRole>().Select(r => $"'{r}'"));
        builder.ToTable("group_memberships", t => t.HasCheckConstraint(
            GroupsDbNames.MembershipsRole,
            $"role IN ({roles})"));

        builder.HasKey(m => new { m.GroupId, m.UserId }).HasName(GroupsDbNames.MembershipsPrimaryKey);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(GroupConfiguration.EnumMaxLength);

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tek grup kuralı
        builder.HasIndex(m => m.UserId, GroupsDbNames.MembershipsUserIdUnique)
            .HasDatabaseName(GroupsDbNames.MembershipsUserIdUnique).IsUnique();

        // Tek yönetici kuralı. Yönetici devrinde önce eski yönetici bırakılmalı (yazma sırası).
        builder.HasIndex(m => m.GroupId, GroupsDbNames.MembershipsOneAdmin)
            .HasDatabaseName(GroupsDbNames.MembershipsOneAdmin)
            .IsUnique()
            .HasFilter($"role = '{MembershipRole.Admin}'");

        // TRG-02: en eski üye
        builder.HasIndex(m => new { m.GroupId, m.JoinedAt }, GroupsDbNames.MembershipsGroupIdJoinedAtIndex)
            .HasDatabaseName(GroupsDbNames.MembershipsGroupIdJoinedAtIndex);
    }
}
