using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Groups;
using Roomly.Api.IntegrationTests.Infrastructure;
using static Roomly.Api.IntegrationTests.Infrastructure.TestData;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed class InviteCodeConstraintTests(PostgresFixture fixture)
{
    [Fact]
    public async Task InsertInviteCode_WhenGroupHasActiveCode_IsRejected_DM08()
    {
        var (admin, group) = await SeedGroupAsync();
        await fixture.SeedAsync(NewInviteCode(group.Id, admin.Id));

        var error = await fixture.InsertRejectedAsync(NewInviteCode(group.Id, admin.Id));

        error.ShouldViolate(DbAssert.UniqueViolation, GroupsDbNames.InviteCodesOneActive);
    }

    [Fact]
    public async Task InsertInviteCode_WhenPreviousCodesRevoked_IsAccepted_DM08()
    {
        var (admin, group) = await SeedGroupAsync();

        await fixture.SeedAsync(
            NewInviteCode(group.Id, admin.Id, revokedAt: Now),
            NewInviteCode(group.Id, admin.Id, revokedAt: Now),
            NewInviteCode(group.Id, admin.Id));

        await using var db = fixture.CreateContext();
        (await db.Set<InviteCode>().CountAsync(c => c.GroupId == group.Id && c.RevokedAt == null)).Should().Be(1);
    }

    [Fact]
    public async Task InsertInviteCode_WhenHashUsedByAnotherGroup_IsRejected_DM08()
    {
        var (admin, group) = await SeedGroupAsync();
        var (otherAdmin, otherGroup) = await SeedGroupAsync();
        var hash = RandomHash();
        await fixture.SeedAsync(NewInviteCode(group.Id, admin.Id, hash));

        var error = await fixture.InsertRejectedAsync(NewInviteCode(otherGroup.Id, otherAdmin.Id, hash));

        error.ShouldViolate(DbAssert.UniqueViolation, GroupsDbNames.InviteCodesCodeHashUnique);
    }

    [Fact]
    public async Task RegenerateInviteCode_RevokeThenInsertInOneTransaction_Succeeds_DM08()
    {
        var (admin, group) = await SeedGroupAsync();
        var oldCode = NewInviteCode(group.Id, admin.Id);
        await fixture.SeedAsync(oldCode);
        var newCode = NewInviteCode(group.Id, admin.Id);

        // Belgelenen yazma sırası: kilit, ayrı UPDATE ile eski kodu iptal, sonra INSERT, commit.
        await using (var db = fixture.CreateContext())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Set<Group>()
                .FromSqlInterpolated($"SELECT * FROM groups WHERE id = {group.Id} FOR UPDATE")
                .SingleAsync();
            await db.Set<InviteCode>()
                .Where(c => c.GroupId == group.Id && c.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.RevokedAt, Now));
            db.Add(newCode);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var verify = fixture.CreateContext();
        var codes = await verify.Set<InviteCode>().Where(c => c.GroupId == group.Id).ToListAsync();
        codes.Should().HaveCount(2);
        codes.Single(c => c.RevokedAt == null).Id.Should().Be(newCode.Id);
        codes.Single(c => c.Id == oldCode.Id).RevokedAt.Should().Be(Now);
    }

    [Fact]
    public async Task InsertInviteCode_WithHashNot32Bytes_IsRejected_DM07()
    {
        var (admin, group) = await SeedGroupAsync();

        var error = await fixture.InsertRejectedAsync(NewInviteCode(group.Id, admin.Id, RandomHash(16)));

        error.ShouldViolate(DbAssert.CheckViolation, GroupsDbNames.InviteCodesCodeHashLength);
    }

    private async Task<(Roomly.Api.Features.Auth.AppUser Admin, Group Group)> SeedGroupAsync()
    {
        var admin = NewUser();
        var group = NewGroup();
        await fixture.SeedAsync(admin, group, NewMembership(group.Id, admin.Id, MembershipRole.Admin));
        return (admin, group);
    }
}
