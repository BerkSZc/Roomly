using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.Auth;

[Collection(PostgresCollection.Name)]
public sealed class RateLimitTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Auth_EleventhRequestInWindow_Returns429_SEC04()
    {
        // Gerçek limit (10 / dk) ile ayrı bir API örneği; bütün test istekleri aynı "IP"den gelir.
        var client = ApiFactory.For(fixture, authPermitLimit: 10).CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            statuses.Add((await client.LoginAsync(AuthApi.UniqueEmail())).StatusCode);
        }

        statuses.Take(10).Should().AllSatisfy(s => s.Should().Be(HttpStatusCode.Unauthorized));
        statuses[10].Should().Be(HttpStatusCode.TooManyRequests);
        var last = await client.LoginAsync(AuthApi.UniqueEmail());
        (await last.ErrorCodeAsync()).Should().Be("RATE_LIMITED");
    }

    [Fact]
    public async Task MeAndLogout_AreNotRateLimited_WhileLoginIs_SEC04()
    {
        // Aynı Wi-Fi'daki ev arkadaşları tek IP paylaşır; uygulama açılışındaki /me sınıra takılmamalı.
        var client = ApiFactory.For(fixture, authPermitLimit: 1).CreateClient();
        var auth = await client.RegisterOkAsync();

        for (var i = 0; i < 5; i++)
        {
            (await client.MeAsync(auth.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = auth.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await client.LoginAsync(AuthApi.UniqueEmail());
        login.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
