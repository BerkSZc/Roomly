using System.Net;
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
}
