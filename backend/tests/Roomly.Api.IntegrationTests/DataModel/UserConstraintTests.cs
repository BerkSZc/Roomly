using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth;
using Roomly.Api.IntegrationTests.Infrastructure;
using static Roomly.Api.IntegrationTests.Infrastructure.TestData;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed class UserConstraintTests(PostgresFixture fixture)
{
    [Fact]
    public async Task InsertUser_WithDisplayNameOver50Chars_IsRejected_DM10()
    {
        var error = await fixture.InsertRejectedAsync(NewUser(new string('a', 51)));

        error.SqlState.Should().Be(DbAssert.StringTooLong);
    }

    [Fact]
    public async Task InsertUser_WithAnonymizedDisplayName_IsAccepted_DM10()
    {
        var user = NewUser();
        user.DisplayName = $"DeletedUser_{user.Id}";
        user.DeletedAt = Now;

        await fixture.SeedAsync(user);

        await using var db = fixture.CreateContext();
        var saved = await db.Set<AppUser>().SingleAsync(u => u.Id == user.Id);
        saved.DisplayName.Should().HaveLength(48);
        saved.DeletedAt.Should().Be(Now);
    }

    [Fact]
    public async Task InsertUser_WithoutDeletedAt_IsAccepted_DM10()
    {
        var user = NewUser();

        await fixture.SeedAsync(user);

        await using var db = fixture.CreateContext();
        (await db.Set<AppUser>().SingleAsync(u => u.Id == user.Id)).DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task InsertUser_WithExistingNormalizedEmail_IsRejected_DM10()
    {
        var email = $"ayni{Guid.NewGuid():N}@test.local";
        await fixture.SeedAsync(NewUser(email: email));
        var duplicate = NewUser(email: email);

        var error = await fixture.InsertRejectedAsync(duplicate);

        // İki benzersiz index aynı satırda; PostgreSQL ilk ihlal edileni bildirir.
        error.SqlState.Should().Be(DbAssert.UniqueViolation);
        error.ConstraintName.Should().BeOneOf(
            AuthDbNames.UsersNormalizedEmailUnique, AuthDbNames.UsersNormalizedUserNameUnique);
    }

    [Fact]
    public async Task InsertUser_WithExistingNormalizedEmailButNewUserName_IsRejectedByEmailIndex_DM10()
    {
        var email = $"ayni{Guid.NewGuid():N}@test.local";
        await fixture.SeedAsync(NewUser(email: email));
        var duplicate = NewUser(email: email);
        duplicate.UserName = $"baska{Guid.NewGuid():N}";
        duplicate.NormalizedUserName = duplicate.UserName.ToUpperInvariant();

        var error = await fixture.InsertRejectedAsync(duplicate);

        error.ShouldViolate(DbAssert.UniqueViolation, AuthDbNames.UsersNormalizedEmailUnique);
    }
}
