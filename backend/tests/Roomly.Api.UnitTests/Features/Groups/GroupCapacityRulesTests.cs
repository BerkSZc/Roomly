using FluentAssertions;
using Roomly.Api.Features.Groups;

namespace Roomly.Api.UnitTests.Features.Groups;

public sealed class GroupCapacityRulesTests
{
    [Theory]
    [InlineData(GroupType.Ev, 1, false)]
    [InlineData(GroupType.Ev, 2, true)]
    [InlineData(GroupType.Ev, 20, true)]
    [InlineData(GroupType.Ev, 21, false)]
    [InlineData(GroupType.Yurt, 1, false)]
    [InlineData(GroupType.Yurt, 2, true)]
    [InlineData(GroupType.Yurt, 6, true)]
    [InlineData(GroupType.Yurt, 7, false)]
    public void IsValid_AtAndAroundTypeBoundaries_MatchesBnd02_BND02(GroupType type, int capacity, bool expected)
    {
        GroupCapacityRules.IsValid(type, capacity).Should().Be(expected);
    }

    [Fact]
    public void CheckConstraintSql_IsBuiltFromSameRanges_BND02()
    {
        GroupCapacityRules.CheckConstraintSql().Should().Be(
            "(type = 'Ev' AND capacity BETWEEN 2 AND 20) OR (type = 'Yurt' AND capacity BETWEEN 2 AND 6)");
    }
}
