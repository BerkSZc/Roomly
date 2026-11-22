using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Groups;
using Roomly.Api.IntegrationTests.Infrastructure;
using static Roomly.Api.IntegrationTests.Infrastructure.TestData;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed class DataModelRoundTripTests(PostgresFixture fixture)
{
    [Fact]
    public async Task WriteGroupWithAdminMemberAndCode_ThroughSetOfT_ReadsBackSameValues_DM12()
    {
        var admin = NewUser();
        var member = NewUser();
        var group = NewGroup(GroupType.Yurt, 4);
        var code = NewInviteCode(group.Id, admin.Id, InviteCodeHasher.Hash("ABCD-EFGH-2345"));

        await using (var db = fixture.CreateContext())
        {
            db.Set<Roomly.Api.Features.Auth.AppUser>().AddRange(admin, member);
            db.Set<Group>().Add(group);
            await db.SaveChangesAsync();
            db.Set<GroupMembership>().AddRange(
                NewMembership(group.Id, admin.Id, MembershipRole.Admin),
                NewMembership(group.Id, member.Id));
            db.Set<InviteCode>().Add(code);
            await db.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var savedGroup = await verify.Set<Group>().SingleAsync(g => g.Id == group.Id);
        var memberships = await verify.Set<GroupMembership>().Where(m => m.GroupId == group.Id).ToListAsync();
        var savedCode = await verify.Set<InviteCode>().SingleAsync(c => c.GroupId == group.Id);

        savedGroup.Should().BeEquivalentTo(group);
        memberships.Should().ContainSingle(m => m.Role == MembershipRole.Admin).Which.UserId.Should().Be(admin.Id);
        memberships.Should().ContainSingle(m => m.Role == MembershipRole.Member).Which.UserId.Should().Be(member.Id);
        savedCode.CodeHash.Should().Equal(InviteCodeHasher.Hash("abcd efgh 2345"));
    }

    [Fact]
    public async Task WriteTimestamps_RoundTripAsUtc_DM13()
    {
        var group = NewGroup();

        await fixture.SeedAsync(group);

        await using var db = fixture.CreateContext();
        var saved = await db.Set<Group>().SingleAsync(g => g.Id == group.Id);
        saved.CreatedAt.Should().Be(Now);
        saved.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }
}
