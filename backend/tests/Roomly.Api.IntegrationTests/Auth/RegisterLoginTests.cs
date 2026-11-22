using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Auth.Dtos;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.Auth;

[Collection(PostgresCollection.Name)]
public sealed class RegisterLoginTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = ApiFactory.For(fixture).CreateClient();

    [Fact]
    public async Task Register_WithValidInput_Returns201AndStoresUser_AUTH01()
    {
        var email = $"  Yeni{Guid.NewGuid():N}@Test.Local ";

        var response = await _client.RegisterAsync(email, displayName: "  Berk  ");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        body.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().HaveLength(43);
        body.User.DisplayName.Should().Be("Berk");
        body.User.Email.Should().Be(email.Trim().ToLowerInvariant());

        await using var db = fixture.CreateContext();
        var user = await db.Set<AppUser>().SingleAsync(u => u.Id == body.User.Id);
        user.UserName.Should().Be(user.Email);
        user.PasswordHash.Should().NotBeNullOrEmpty().And.NotContain(AuthApi.ValidPassword);
    }

    [Theory]
    [InlineData("B", "ok@test.local", "Sifre1234", "displayName")]
    [InlineData("Berk", "gecersiz-eposta", "Sifre1234", "email")]
    [InlineData("Berk", "ok@test.local", "sadeceharf", "password")]
    [InlineData("Berk", "ok@test.local", "12345678", "password")]
    [InlineData("Berk", "ok@test.local", "Kisa123", "password")]
    public async Task Register_WithInvalidField_Returns400WithFieldError_AUTH02(
        string displayName, string email, string password, string field)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(displayName, email, password));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        body.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithTakenEmailDifferentCase_Returns409_AUTH03()
    {
        var email = AuthApi.UniqueEmail();
        await _client.RegisterOkAsync(email);

        var response = await _client.RegisterAsync(email.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ErrorCodeAsync()).Should().Be(AuthErrors.EmailTakenCode);
    }

    [Fact]
    public async Task Register_ConcurrentSameEmail_OneSucceedsOtherGets409_AUTH03()
    {
        var email = AuthApi.UniqueEmail();

        var responses = await Task.WhenAll(_client.RegisterAsync(email), _client.RegisterAsync(email));

        responses.Select(r => r.StatusCode).Should()
            .BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Returns200_AUTH04()
    {
        var email = AuthApi.UniqueEmail();
        await _client.RegisterOkAsync(email);

        var response = await _client.LoginAsync(email.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        (await _client.MeAsync(body.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongPasswordUnknownOrDeletedUser_ReturnSame401_AUTH04()
    {
        var email = AuthApi.UniqueEmail();
        var registered = await _client.RegisterOkAsync(email);
        var deletedEmail = AuthApi.UniqueEmail();
        var deleted = await _client.RegisterOkAsync(deletedEmail);
        await using (var db = fixture.CreateContext())
        {
            await db.Set<AppUser>().Where(u => u.Id == deleted.User.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.DeletedAt, TestData.Now));
        }

        var wrong = await _client.LoginAsync(email, "Yanlis1234");
        var unknown = await _client.LoginAsync(AuthApi.UniqueEmail());
        var gone = await _client.LoginAsync(deletedEmail);

        foreach (var response in new[] { wrong, unknown, gone })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await response.ErrorCodeAsync()).Should().Be(AuthErrors.InvalidCredentialsCode);
        }

        registered.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_AfterFiveFailures_LocksAccountUntilLockoutEnds_SEC01()
    {
        var email = AuthApi.UniqueEmail();
        var registered = await _client.RegisterOkAsync(email);

        var codes = new List<string?>();
        for (var i = 0; i < 5; i++)
        {
            codes.Add(await (await _client.LoginAsync(email, "Yanlis1234")).ErrorCodeAsync());
        }

        codes.Should().Equal(
            AuthErrors.InvalidCredentialsCode, AuthErrors.InvalidCredentialsCode, AuthErrors.InvalidCredentialsCode,
            AuthErrors.InvalidCredentialsCode, AuthErrors.AccountLockedCode);

        var whileLocked = await _client.LoginAsync(email);
        whileLocked.StatusCode.Should().Be(HttpStatusCode.Locked);
        (await whileLocked.ErrorCodeAsync()).Should().Be(AuthErrors.AccountLockedCode);

        await using (var db = fixture.CreateContext())
        {
            var user = await db.Set<AppUser>().SingleAsync(u => u.Id == registered.User.Id);
            user.LockoutEnd.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
            var past = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.Set<AppUser>().Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LockoutEnd, past));
        }

        (await _client.LoginAsync(email)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthLogs_NeverContainPlainEmailPasswordOrToken_SEC07()
    {
        var email = AuthApi.UniqueEmail();
        var registered = await _client.RegisterOkAsync(email);
        await _client.LoginAsync(email, "Yanlis1234");
        await _client.LoginAsync(email);
        await _client.RegisterAsync(email);

        var lines = ApiFactory.Logs.Lines.ToList();
        lines.Should().NotContain(l => l.Contains(email, StringComparison.OrdinalIgnoreCase));
        lines.Should().NotContain(l => l.Contains(AuthApi.ValidPassword) || l.Contains("Yanlis1234"));
        lines.Should().NotContain(l => l.Contains(registered.RefreshToken) || l.Contains(registered.AccessToken));
        lines.Should().Contain(l => l.Contains(Roomly.Api.Common.Logging.LogMask.Email(email)));
    }
}
