using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Roomly.Api.Features.Groups;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    /// <summary>Enum sütunlarının (tür, rol) en fazla uzunluğu.</summary>
    internal const int EnumMaxLength = 10;

    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups", t => t.HasCheckConstraint(
            GroupsDbNames.GroupsCapacityByType,
            GroupCapacityRules.CheckConstraintSql()));

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).HasMaxLength(Group.NameMaxLength).IsRequired();
        builder.Property(g => g.Type).HasConversion<string>().HasMaxLength(EnumMaxLength);
    }
}
