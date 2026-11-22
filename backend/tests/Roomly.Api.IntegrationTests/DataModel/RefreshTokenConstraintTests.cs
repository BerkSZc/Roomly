using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth;
using Roomly.Api.IntegrationTests.Infrastructure;
using static Roomly.Api.IntegrationTests.Infrastructure.TestData;

namespace Roomly.Api.IntegrationTests.DataModel;

[Collection(PostgresCollection.Name)]
public sealed class RefreshTokenConstraintTests(PostgresFixture fixture)
{
    [Fact]
    public async Task RotateRefreshToken_ChainInSameFamily_IsStored_DM09()
    {
        var user = NewUser();
        var family = Guid.NewGuid();
        var second = NewRefreshToken(user.Id, family);
        var first = NewRefreshToken(user.Id, family, replacedById: second.Id);
        first.RevokedAt = Now;

        await fixture.SeedAsync(user, second, first);

        await using var db = fixture.CreateContext();
        var tokens = await db.Set<RefreshToken>().Where(t => t.FamilyId == family).ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Single(t => t.Id == first.Id).ReplacedById.Should().Be(second.Id);
        tokens.Should().AllSatisfy(t => t.TokenHash.Should().HaveCount(RefreshTokenRules.HashLength));
    }

    [Fact]
    public async Task InsertRefreshToken_WithDuplicateHash_IsRejected_DM09()
    {
        var user = NewUser();
        var hash = RandomHash();
        await fixture.SeedAsync(user, NewRefreshToken(user.Id, tokenHash: hash));

        var error = await fixture.InsertRejectedAsync(NewRefreshToken(user.Id, tokenHash: hash));

        error.ShouldViolate(DbAssert.UniqueViolation, AuthDbNames.RefreshTokensTokenHashUnique);
    }

    [Fact]
    public async Task InsertRefreshToken_ReplacingAlreadyReplacedToken_IsRejected_DM09()
    {
        var user = NewUser();
        var replacement = NewRefreshToken(user.Id);
        await fixture.SeedAsync(user, replacement, NewRefreshToken(user.Id, replacedById: replacement.Id));

        var error = await fixture.InsertRejectedAsync(NewRefreshToken(user.Id, replacedById: replacement.Id));

        error.ShouldViolate(DbAssert.UniqueViolation, AuthDbNames.RefreshTokensReplacedByIdUnique);
    }

    [Fact]
    public async Task InsertRefreshToken_WithHashNot32Bytes_IsRejected_DM09()
    {
        var user = NewUser();
        await fixture.SeedAsync(user);

        var error = await fixture.InsertRejectedAsync(NewRefreshToken(user.Id, tokenHash: RandomHash(31)));

        error.ShouldViolate(DbAssert.CheckViolation, AuthDbNames.RefreshTokensTokenHashLength);
    }
}
