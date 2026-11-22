using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Roomly.Api.Common.Results;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Groups;
using Roomly.Api.Features.Groups.Dtos;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.Groups;

/// <summary>Katılım kuralları gerçek Postgres üzerinde, zaman <see cref="FakeClock"/> ile (spec 0006).</summary>
[Collection(PostgresCollection.Name)]
public sealed class JoinGroupServiceTests(PostgresFixture fixture)
{
    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private sealed record Setup(Guid GroupId, Guid AdminId, string Code, Guid InviteId);

    private static string NewCode() =>
        "T" + Convert.ToHexString(Guid.NewGuid().ToByteArray())[..11];

    private async Task<Setup> GroupWithInviteAsync(
        int capacity = 5, DateTimeOffset? revokedAt = null, DateTimeOffset? closedAt = null)
    {
        var admin = TestData.NewUser("Yönetici");
        var group = TestData.NewGroup(capacity: capacity);
        group.ClosedAt = closedAt;
        var code = NewCode();
        var invite = TestData.NewInviteCode(group.Id, admin.Id, InviteCodeHasher.Hash(code), revokedAt);
        await fixture.SeedAsync(admin, group, TestData.NewMembership(group.Id, admin.Id, MembershipRole.Admin), invite);
        return new Setup(group.Id, admin.Id, code, invite.Id);
    }

    private async Task<Guid> NewUserAsync()
    {
        var user = TestData.NewUser();
        await fixture.SeedAsync(user);
        return user.Id;
    }

    private Task<Result<JoinGroupResponse>> JoinAsync(Guid userId, string code, DateTimeOffset? at = null)
    {
        var db = fixture.CreateContext();
        var service = new JoinGroupService(
            new JoinGroupRepository(db), new FakeClock(at ?? TestData.Now.AddHours(1)), NullLogger<JoinGroupService>.Instance);
        return service.JoinAsync(userId, code, CancellationToken.None)
            .ContinueWith(t => { db.Dispose(); return t.Result; });
    }

    private async Task<int> MemberCountAsync(Guid groupId)
    {
        await using var db = fixture.CreateContext();
        return await db.Set<GroupMembership>().CountAsync(m => m.GroupId == groupId);
    }

    [Fact]
    public async Task Join_WithValidCode_AddsMemberAndKeepsCodeReusable_JOIN01()
    {
        var setup = await GroupWithInviteAsync();
        var first = await NewUserAsync();
        var second = await NewUserAsync();
        var at = TestData.Now.AddHours(3);

        var result = await JoinAsync(first, setup.Code, at);
        var again = await JoinAsync(second, setup.Code, at);

        result.IsSuccess.Should().BeTrue();
        result.Value.Group.Id.Should().Be(setup.GroupId);
        result.Value.Group.Role.Should().Be(MembershipRole.Member);
        again.IsSuccess.Should().BeTrue();

        await using var db = fixture.CreateContext();
        var membership = await db.Set<GroupMembership>().SingleAsync(m => m.UserId == first);
        membership.Role.Should().Be(MembershipRole.Member);
        membership.JoinedAt.Should().Be(at);
        (await db.Set<InviteCode>().SingleAsync(i => i.Id == setup.InviteId)).RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Join_CodeIsNormalizedBeforeLookup_JOIN02()
    {
        var setup = await GroupWithInviteAsync();
        var spaced = $" {setup.Code[..4].ToLowerInvariant()}-{setup.Code[4..8]} {setup.Code[8..]}\t";

        (await JoinAsync(await NewUserAsync(), spaced)).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("KOD!KOD!KOD")]
    [InlineData("ÇOKGÜZELKOD1")]
    public async Task Join_WithMalformedCode_ReturnsInviteInvalid_JOIN02(string code)
    {
        var result = await JoinAsync(await NewUserAsync(), code);

        result.Error!.Code.Should().Be(GroupErrors.InviteInvalidCode);
    }

    [Fact]
    public async Task Join_WithUnknownCodeOrClosedGroup_ReturnsInviteInvalid_JOIN03()
    {
        var closed = await GroupWithInviteAsync(closedAt: TestData.Now);

        (await JoinAsync(await NewUserAsync(), NewCode())).Error!.Code.Should().Be(GroupErrors.InviteInvalidCode);
        (await JoinAsync(await NewUserAsync(), closed.Code)).Error!.Code.Should().Be(GroupErrors.InviteInvalidCode);
    }

    [Fact]
    public async Task Join_AtExactlyFortyEightHours_Succeeds_BND03()
    {
        var setup = await GroupWithInviteAsync();

        var result = await JoinAsync(await NewUserAsync(), setup.Code, TestData.Now + InviteCodeRules.Lifetime);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Join_OneSecondAfterExpiry_ReturnsInviteExpired_BND03()
    {
        var setup = await GroupWithInviteAsync();

        var result = await JoinAsync(
            await NewUserAsync(), setup.Code, TestData.Now + InviteCodeRules.Lifetime + TimeSpan.FromSeconds(1));

        result.Error!.Code.Should().Be(GroupErrors.InviteExpiredCode);
        result.Error.Status.Should().Be(410);
    }

    [Fact]
    public async Task Join_WithRevokedCode_ReturnsInviteRevokedEvenIfExpired_JOIN05()
    {
        var setup = await GroupWithInviteAsync(revokedAt: TestData.Now.AddHours(1));
        var user = await NewUserAsync();

        (await JoinAsync(user, setup.Code)).Error!.Code.Should().Be(GroupErrors.InviteRevokedCode);
        (await JoinAsync(user, setup.Code, TestData.Now.AddDays(5))).Error!.Code
            .Should().Be(GroupErrors.InviteRevokedCode);
    }

    [Fact]
    public async Task Join_WhenAlreadyMember_ReturnsAlreadyMemberEvenWithExpiredCode_JOIN06()
    {
        var setup = await GroupWithInviteAsync();

        var result = await JoinAsync(setup.AdminId, setup.Code, TestData.Now.AddDays(5));

        result.Error!.Code.Should().Be(GroupErrors.AlreadyMemberCode);
        result.Error.Message.Should().Be("Zaten bu grubun üyesisiniz.");
        result.Error.Status.Should().Be(409);
    }

    [Fact]
    public async Task Join_WhenMemberOfAnotherGroup_ReturnsAlreadyInGroup_JOIN06()
    {
        var mine = await GroupWithInviteAsync();
        var other = await GroupWithInviteAsync();

        var result = await JoinAsync(mine.AdminId, other.Code);

        result.Error!.Code.Should().Be(GroupErrors.AlreadyInGroupCode);
    }

    [Fact]
    public async Task Join_WhenGroupIsFull_ReturnsCapacityExceeded_BND01()
    {
        var setup = await GroupWithInviteAsync(capacity: 2);
        (await JoinAsync(await NewUserAsync(), setup.Code)).IsSuccess.Should().BeTrue();

        var result = await JoinAsync(await NewUserAsync(), setup.Code);

        result.Error!.Code.Should().Be(GroupErrors.CapacityExceededCode);
        (await MemberCountAsync(setup.GroupId)).Should().Be(2);
    }

    [Fact]
    public async Task Join_ConcurrentRequests_NeverExceedCapacity_BND01()
    {
        var setup = await GroupWithInviteAsync(capacity: 2);
        var users = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            users.Add(await NewUserAsync());
        }

        var results = await Task.WhenAll(users.Select(u => Task.Run(() => JoinAsync(u, setup.Code))));

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Where(r => !r.IsSuccess).Should()
            .AllSatisfy(r => r.Error!.Code.Should().Be(GroupErrors.CapacityExceededCode));
        (await MemberCountAsync(setup.GroupId)).Should().Be(2);
    }

    [Fact]
    public async Task Join_SameUserConcurrently_SecondGetsAlreadyMember_TRG01()
    {
        var setup = await GroupWithInviteAsync();
        var user = await NewUserAsync();

        var results = await Task.WhenAll(
            Task.Run(() => JoinAsync(user, setup.Code)), Task.Run(() => JoinAsync(user, setup.Code)));

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Single(r => !r.IsSuccess).Error!.Code.Should().Be(GroupErrors.AlreadyMemberCode);
    }

    [Fact]
    public async Task Join_SameUserIntoTwoGroupsConcurrently_OnlyOneSucceeds_TRG01()
    {
        var first = await GroupWithInviteAsync();
        var second = await GroupWithInviteAsync();
        var user = await NewUserAsync();

        var results = await Task.WhenAll(
            Task.Run(() => JoinAsync(user, first.Code)), Task.Run(() => JoinAsync(user, second.Code)));

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Single(r => !r.IsSuccess).Error!.Code.Should().Be(GroupErrors.AlreadyInGroupCode);
    }
}
