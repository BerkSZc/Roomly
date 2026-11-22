using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Groups;
using Roomly.Api.IntegrationTests.Infrastructure;
using static Roomly.Api.IntegrationTests.Infrastructure.TestData;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed class DeleteBehaviorTests(PostgresFixture fixture)
{
    [Fact]
    public async Task DeleteGroup_RemovesMembershipsAndInviteCodes_DM11()
    {
        var admin = NewUser();
        var member = NewUser();
        var group = NewGroup();
        await fixture.SeedAsync(
            admin, member, group,
            NewMembership(group.Id, admin.Id, MembershipRole.Admin),
            NewMembership(group.Id, member.Id),
            NewInviteCode(group.Id, admin.Id, revokedAt: Now),
            NewInviteCode(group.Id, admin.Id));

        await using (var db = fixture.CreateContext())
        {
            await db.Set<Group>().Where(g => g.Id == group.Id).ExecuteDeleteAsync();
        }

        await using var verify = fixture.CreateContext();
        (await verify.Set<GroupMembership>().AnyAsync(m => m.GroupId == group.Id)).Should().BeFalse();
        (await verify.Set<InviteCode>().AnyAsync(c => c.GroupId == group.Id)).Should().BeFalse();
        (await verify.Set<AppUser>().CountAsync(u => u.Id == admin.Id || u.Id == member.Id)).Should().Be(2);
    }

    [Fact]
    public async Task DeleteUser_WithMembership_IsRejected_DM11()
    {
        var user = NewUser();
        var group = NewGroup();
        await fixture.SeedAsync(user, group, NewMembership(group.Id, user.Id));

        var error = await DeleteUserRejectedAsync(user.Id);

        error.SqlState.Should().Be(DbAssert.ForeignKeyViolation);
        error.ConstraintName.Should().Be("fk_group_memberships_asp_net_users_user_id");
    }

    [Fact]
    public async Task DeleteUser_WhoCreatedInviteCode_IsRejected_DM11()
    {
        var creator = NewUser();
        var group = NewGroup();
        await fixture.SeedAsync(creator, group, NewInviteCode(group.Id, creator.Id));

        var error = await DeleteUserRejectedAsync(creator.Id);

        error.SqlState.Should().Be(DbAssert.ForeignKeyViolation);
        error.ConstraintName.Should().Be("fk_invite_codes_asp_net_users_created_by_user_id");
    }

    [Fact]
    public async Task CloseGroup_WithClosedAt_KeepsRow_DM11()
    {
        var group = NewGroup();
        await fixture.SeedAsync(group);

        await using (var db = fixture.CreateContext())
        {
            await db.Set<Group>().Where(g => g.Id == group.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.ClosedAt, Now));
        }

        await using var verify = fixture.CreateContext();
        (await verify.Set<Group>().SingleAsync(g => g.Id == group.Id)).ClosedAt.Should().Be(Now);
    }

    private Task<Npgsql.PostgresException> DeleteUserRejectedAsync(Guid userId) =>
        DbAssert.RejectedAsync(async () =>
        {
            await using var db = fixture.CreateContext();
            await db.Set<AppUser>().Where(u => u.Id == userId).ExecuteDeleteAsync();
        });
}
