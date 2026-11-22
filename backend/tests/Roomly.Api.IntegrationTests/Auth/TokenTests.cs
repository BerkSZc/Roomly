using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Auth.Dtos;
using Roomly.Api.Features.Groups;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.Auth;

[Collection(PostgresCollection.Name)]
public sealed class TokenTests(PostgresFixture fixture)
{
    private readonly ApiFactory _factory = ApiFactory.For(fixture);

    private HttpClient Client => _factory.CreateClient();

    [Fact]
    public async Task AccessToken_HasExpectedClaimsAndFifteenMinuteLifetime_AUTH07()
    {
        var auth = await Client.RegisterOkAsync();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);

        jwt.Header.Alg.Should().Be("HS256");
        jwt.Subject.Should().Be(auth.User.Id.ToString());
        jwt.Id.Should().NotBeNullOrEmpty();
        jwt.Issuer.Should().Be("roomly-api");
        jwt.Audiences.Should().Equal("roomly-mobile");
        (jwt.ValidTo - jwt.ValidFrom).Should().Be(TimeSpan.FromMinutes(15));
        auth.AccessTokenExpiresAt.Should().BeCloseTo(new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMissingInvalidOrExpiredToken_Returns401_AUTH07()
    {
        var auth = await Client.RegisterOkAsync();
        var past = new FixedClock(DateTimeOffset.UtcNow.AddMinutes(-20));
        var expired = new JwtTokenIssuer(_factory.GetService<IOptions<JwtOptions>>(), past).Issue(auth.User.Id);

        var missing = await Client.GetAsync("/api/auth/me");
        var garbage = await Client.MeAsync("bozuk.token.degeri");
        var old = await Client.MeAsync(expired.Value);

        foreach (var response in new[] { missing, garbage, old })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await response.ErrorCodeAsync()).Should().Be("UNAUTHORIZED");
        }
    }

    [Fact]
    public async Task Register_StoresOnlyHashOfRefreshTokenWithSevenDayExpiry_AUTH08()
    {
        var auth = await Client.RegisterOkAsync();

        await using var db = fixture.CreateContext();
        var row = await db.Set<RefreshToken>().SingleAsync(t => t.UserId == auth.User.Id);
        row.TokenHash.Should().Equal(RefreshTokenGenerator.Hash(auth.RefreshToken));
        (row.ExpiresAt - row.CreatedAt).Should().Be(TimeSpan.FromDays(7));
        row.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_RotatesTokenWithinSameFamily_AUTH09()
    {
        var auth = await Client.RegisterOkAsync();

        var response = await Client.RefreshAsync(auth.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        rotated.RefreshToken.Should().NotBe(auth.RefreshToken);

        await using var db = fixture.CreateContext();
        var rows = await db.Set<RefreshToken>().Where(t => t.UserId == auth.User.Id).ToListAsync();
        var old = rows.Single(t => t.TokenHash.SequenceEqual(RefreshTokenGenerator.Hash(auth.RefreshToken)));
        var fresh = rows.Single(t => t.TokenHash.SequenceEqual(RefreshTokenGenerator.Hash(rotated.RefreshToken)));
        old.RevokedAt.Should().NotBeNull();
        old.ReplacedById.Should().Be(fresh.Id);
        fresh.FamilyId.Should().Be(old.FamilyId);
        fresh.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_WithExpiredOrUnknownToken_Returns401_AUTH09()
    {
        var auth = await Client.RegisterOkAsync();
        await using (var db = fixture.CreateContext())
        {
            var past = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.Set<RefreshToken>().Where(t => t.UserId == auth.User.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, past));
        }

        var expired = await Client.RefreshAsync(auth.RefreshToken);
        var unknown = await Client.RefreshAsync(RefreshTokenGenerator.Generate().PlainText);

        foreach (var response in new[] { expired, unknown })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await response.ErrorCodeAsync()).Should().Be(AuthErrors.InvalidRefreshTokenCode);
        }
    }

    [Fact]
    public async Task Refresh_WithAlreadyRotatedToken_RevokesWholeFamily_AUTH10()
    {
        var auth = await Client.RegisterOkAsync();
        var rotated = (await (await Client.RefreshAsync(auth.RefreshToken)).Content.ReadFromJsonAsync<AuthResponse>())!;

        var reuse = await Client.RefreshAsync(auth.RefreshToken);

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await reuse.ErrorCodeAsync()).Should().Be(AuthErrors.InvalidRefreshTokenCode);
        (await Client.RefreshAsync(rotated.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ConcurrentWithSameToken_OnlyOneSucceeds_AUTH10()
    {
        var auth = await Client.RegisterOkAsync();

        var responses = await Task.WhenAll(
            Client.RefreshAsync(auth.RefreshToken), Client.RefreshAsync(auth.RefreshToken));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
    }

    [Fact]
    public async Task Logout_RevokesFamilyAndIsIdempotent_AUTH11()
    {
        var auth = await Client.RegisterOkAsync();

        var first = await Client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(auth.RefreshToken));
        var again = await Client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(auth.RefreshToken));
        var unknown = await Client.PostAsJsonAsync(
            "/api/auth/logout", new RefreshRequest(RefreshTokenGenerator.Generate().PlainText));

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        unknown.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Client.RefreshAsync(auth.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ReturnsUserAndGroupOrNull_AUTH12()
    {
        var loner = await Client.RegisterOkAsync();
        var member = await Client.RegisterOkAsync();
        var group = TestData.NewGroup(GroupType.Yurt, 4);
        await fixture.SeedAsync(group, TestData.NewMembership(group.Id, member.User.Id, MembershipRole.Admin));

        var lonerMe = (await (await Client.MeAsync(loner.AccessToken)).Content.ReadFromJsonAsync<MeResponse>(Json))!;
        var memberMe = (await (await Client.MeAsync(member.AccessToken)).Content.ReadFromJsonAsync<MeResponse>(Json))!;

        lonerMe.Group.Should().BeNull();
        lonerMe.Email.Should().Be(loner.User.Email);
        memberMe.Group.Should().Be(new GroupSummaryDto(group.Id, group.Name, GroupType.Yurt, MembershipRole.Admin));
    }

    private static readonly System.Text.Json.JsonSerializerOptions Json = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
