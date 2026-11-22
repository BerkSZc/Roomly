using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Roomly.Api.Features.Auth.Dtos;
using Roomly.Api.Features.Groups;
using Roomly.Api.Features.Groups.Dtos;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.Groups;

[Collection(PostgresCollection.Name)]
public sealed class JoinGroupEndpointTests(PostgresFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = ApiFactory.For(fixture).CreateClient();

    private async Task<HttpResponseMessage> JoinAsync(string accessToken, string? code)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/groups/join")
        {
            Content = JsonContent.Create(new JoinGroupRequest(code)),
        };
        request.Headers.Authorization = new("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    /// <summary>Gerçek zamanda geçerli (az önce üretilmiş) bir kodu olan grup.</summary>
    private async Task<(Guid GroupId, string Code)> GroupWithFreshInviteAsync()
    {
        var admin = TestData.NewUser();
        var group = TestData.NewGroup();
        var code = "H" + Convert.ToHexString(Guid.NewGuid().ToByteArray())[..11];
        var invite = TestData.NewInviteCode(group.Id, admin.Id, InviteCodeHasher.Hash(code));
        invite.CreatedAt = DateTimeOffset.UtcNow;
        invite.ExpiresAt = invite.CreatedAt + InviteCodeRules.Lifetime;
        await fixture.SeedAsync(admin, group, TestData.NewMembership(group.Id, admin.Id, MembershipRole.Admin), invite);
        return (group.Id, code);
    }

    [Fact]
    public async Task Join_OverHttp_Returns200AndMeShowsGroup_JOIN01()
    {
        var (groupId, code) = await GroupWithFreshInviteAsync();
        var auth = await _client.RegisterOkAsync();

        var response = await JoinAsync(auth.AccessToken, code.ToLowerInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<JoinGroupResponse>(Json))!;
        body.Group.Id.Should().Be(groupId);
        body.Group.Role.Should().Be(MembershipRole.Member);
        var me = (await (await _client.MeAsync(auth.AccessToken)).Content.ReadFromJsonAsync<MeResponse>(Json))!;
        me.Group!.Id.Should().Be(groupId);
    }

    [Fact]
    public async Task Join_ErrorsCarryCodeAndStatus_JOIN10()
    {
        var auth = await _client.RegisterOkAsync();

        var empty = await JoinAsync(auth.AccessToken, "  ");
        var unknown = await JoinAsync(auth.AccessToken, "YOKBOYLEKOD1");
        var anonymous = await _client.PostAsJsonAsync("/api/groups/join", new JoinGroupRequest("YOKBOYLEKOD1"));

        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await empty.ErrorCodeAsync()).Should().Be("VALIDATION_FAILED");
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await unknown.ErrorCodeAsync()).Should().Be(GroupErrors.InviteInvalidCode);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Join_EleventhRequestPerUserInWindow_Returns429_JOIN10()
    {
        var client = ApiFactory.For(fixture, authPermitLimit: 100_001).WithWebHostBuilder(b =>
            b.UseSetting("RateLimiting:Join:PermitLimit", "10")).CreateClient();
        var auth = await client.RegisterOkAsync();

        HttpResponseMessage last = null!;
        for (var i = 0; i < 11; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/groups/join")
            {
                Content = JsonContent.Create(new JoinGroupRequest("YOKBOYLEKOD1")),
            };
            request.Headers.Authorization = new("Bearer", auth.AccessToken);
            last = await client.SendAsync(request);
        }

        last.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await last.ErrorCodeAsync()).Should().Be("RATE_LIMITED");
    }

    [Fact]
    public async Task JoinLogs_NeverContainPlainCode_JOIN10()
    {
        var (_, code) = await GroupWithFreshInviteAsync();
        var auth = await _client.RegisterOkAsync();

        await JoinAsync(auth.AccessToken, code);

        ApiFactory.Logs.Lines.Should().NotContain(l => l.Contains(code, StringComparison.OrdinalIgnoreCase));
    }
}
