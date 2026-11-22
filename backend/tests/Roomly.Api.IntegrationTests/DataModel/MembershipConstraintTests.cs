using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Groups;
using Roomly.Api.IntegrationTests.Infrastructure;
using static Roomly.Api.IntegrationTests.Infrastructure.TestData;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed class MembershipConstraintTests(PostgresFixture fixture)
{
    [Fact]
    public async Task InsertMembership_WhenSamePairExists_IsRejected_DM03()
    {
        var user = NewUser();
        var group = NewGroup();
        await fixture.SeedAsync(user, group, NewMembership(group.Id, user.Id));

        var error = await fixture.InsertRejectedAsync(NewMembership(group.Id, user.Id));

        error.ShouldViolate(DbAssert.UniqueViolation, GroupsDbNames.MembershipsPrimaryKey);
    }

    [Fact]
    public async Task InsertMembership_WhenUserInAnotherGroup_IsRejected_DM04()
    {
        var user = NewUser();
        var firstGroup = NewGroup();
        var secondGroup = NewGroup();
        await fixture.SeedAsync(user, firstGroup, secondGroup, NewMembership(firstGroup.Id, user.Id));

        var error = await fixture.InsertRejectedAsync(NewMembership(secondGroup.Id, user.Id));

        error.ShouldViolate(DbAssert.UniqueViolation, GroupsDbNames.MembershipsUserIdUnique);
    }

    [Fact]
    public async Task InsertMembership_WhenGroupAlreadyHasAdmin_IsRejected_DM05()
    {
        var admin = NewUser();
        var secondAdmin = NewUser();
        var group = NewGroup();
        await fixture.SeedAsync(admin, secondAdmin, group, NewMembership(group.Id, admin.Id, MembershipRole.Admin));

        var error = await fixture.InsertRejectedAsync(NewMembership(group.Id, secondAdmin.Id, MembershipRole.Admin));

        error.ShouldViolate(DbAssert.UniqueViolation, GroupsDbNames.MembershipsOneAdmin);
    }

    [Fact]
    public async Task InsertMembership_WhenAdminExists_AcceptsMembers_DM05()
    {
        var admin = NewUser();
        var member = NewUser();
        var otherMember = NewUser();
        var group = NewGroup();

        await fixture.SeedAsync(
            admin, member, otherMember, group,
            NewMembership(group.Id, admin.Id, MembershipRole.Admin),
            NewMembership(group.Id, member.Id),
            NewMembership(group.Id, otherMember.Id));

        await using var db = fixture.CreateContext();
        (await db.Set<GroupMembership>().CountAsync(m => m.GroupId == group.Id)).Should().Be(3);
    }

    [Fact]
    public async Task InsertMembership_WithUnknownRole_IsRejected_DM05()
    {
        var user = NewUser();
        var group = NewGroup();
        await fixture.SeedAsync(user, group);

        var error = await DbAssert.RejectedAsync(async () =>
        {
            await using var db = fixture.CreateContext();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO group_memberships (group_id, user_id, role, joined_at) VALUES ({group.Id}, {user.Id}, 'Owner', {Now})");
        });

        error.ShouldViolate(DbAssert.CheckViolation, GroupsDbNames.MembershipsRole);
    }
}
