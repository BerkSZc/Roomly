using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Groups;
using Roomly.Api.IntegrationTests.Infrastructure;
using static Roomly.Api.IntegrationTests.Infrastructure.TestData;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed class GroupCapacityConstraintTests(PostgresFixture fixture)
{
    [Theory]
    [InlineData(GroupType.Ev, 1)]
    [InlineData(GroupType.Ev, 21)]
    [InlineData(GroupType.Yurt, 1)]
    [InlineData(GroupType.Yurt, 7)]
    public async Task InsertGroup_CapacityOutsideTypeRange_IsRejected_BND02(GroupType type, int capacity)
    {
        var error = await fixture.InsertRejectedAsync(NewGroup(type, capacity));

        error.ShouldViolate(DbAssert.CheckViolation, GroupsDbNames.GroupsCapacityByType);
    }

    [Theory]
    [InlineData(GroupType.Ev, 2)]
    [InlineData(GroupType.Ev, 20)]
    [InlineData(GroupType.Yurt, 2)]
    [InlineData(GroupType.Yurt, 6)]
    public async Task InsertGroup_CapacityAtTypeBoundary_IsAccepted_BND02(GroupType type, int capacity)
    {
        var group = NewGroup(type, capacity);

        await fixture.SeedAsync(group);

        await using var db = fixture.CreateContext();
        var saved = await db.Set<Group>().SingleAsync(g => g.Id == group.Id);
        saved.Type.Should().Be(type);
        saved.Capacity.Should().Be(capacity);
    }
}
