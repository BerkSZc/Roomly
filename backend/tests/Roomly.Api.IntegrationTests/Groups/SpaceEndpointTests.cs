using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth.Dtos;
using Roomly.Api.Features.Groups;
using Roomly.Api.Features.Groups.Dtos;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.Groups;

[Collection(PostgresCollection.Name)]
public sealed class SpaceEndpointTests(PostgresFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = ApiFactory.For(fixture).CreateClient();

    [Theory]
    [InlineData("Ev", 1, false)]
    [InlineData("Ev", 2, true)]
    [InlineData("Ev", 20, true)]
    [InlineData("Ev", 21, false)]
    [InlineData("Yurt", 1, false)]
    [InlineData("Yurt", 2, true)]
    [InlineData("Yurt", 6, true)]
    [InlineData("Yurt", 7, false)]
    [InlineData("Invalid", 2, false)]
    [InlineData(null, 2, false)]
    public async Task Create_EnforcesCapacityAndAssignsFounder_BND02(string? type, int capacity, bool accepted)
    {
        var auth = await _client.RegisterOkAsync();
        using var response = await Send(auth, HttpMethod.Post, "/api/groups", new { name = "  Alan  ", type, capacity });
        response.StatusCode.Should().Be(accepted ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
        if (!accepted)
        {
            (await response.ErrorCodeAsync()).Should().Be("VALIDATION_FAILED");
            (await Me(auth)).Group.Should().BeNull();
            return;
        }

        var space = (await response.Content.ReadFromJsonAsync<SpaceDetails>(Json))!;
        space.Name.Should().Be("Alan");
        space.Role.Should().Be(MembershipRole.Admin);
        space.MemberCount.Should().Be(1);
        space.Capacity.Should().Be(capacity);
        (await Me(auth)).Group!.Id.Should().Be(space.Id);
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/groups/{space.Id}");
    }

    [Fact]
    public async Task Create_ConcurrentRequests_ProduceOnlyOneGroup_FR02()
    {
        var auth = await _client.RegisterOkAsync();
        var name = Guid.NewGuid().ToString();
        var body = new { name, type = "Ev", capacity = 2 };
        var responses = await Task.WhenAll(
            Send(auth, HttpMethod.Post, "/api/groups", body),
            Send(auth, HttpMethod.Post, "/api/groups", body));
        try
        {
            responses.Select(r => r.StatusCode).Should().BeEquivalentTo(
                new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
            await using var db = fixture.CreateContext();
            (await db.Set<Group>().CountAsync(g => g.Name == name)).Should().Be(1);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Leave_TransfersByJoinTimeThenUserId_AndArchivesLast_TRG02()
    {
        var admin = await _client.RegisterOkAsync();
        var first = await _client.RegisterOkAsync();
        var second = await _client.RegisterOkAsync();
        var latest = await _client.RegisterOkAsync();
        var outsider = await _client.RegisterOkAsync();
        var space = await Create(admin);
        var tied = new[] { first, second }.OrderBy(a => a.User.Id).ToArray();
        var code = $"H{Guid.NewGuid():N}"[..16];
        var invite = TestData.NewInviteCode(space.Id, admin.User.Id, InviteCodeHasher.Hash(code));
        invite.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        await fixture.SeedAsync(
            TestData.NewMembership(space.Id, first.User.Id),
            TestData.NewMembership(space.Id, second.User.Id),
            TestData.NewMembership(space.Id, latest.User.Id),
            invite);
        await using (var db = fixture.CreateContext())
        {
            await db.Set<GroupMembership>().Where(m => m.GroupId == space.Id && m.UserId == latest.User.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.JoinedAt, TestData.Now.AddMinutes(1)));
        }

        await AssertHidden(outsider, space.Id);
        (await Leave(admin, space.Id)).Should().Be(new LeaveSpaceResponse(false, true));
        (await Me(tied[0])).Group!.Role.Should().Be(MembershipRole.Admin);
        (await Me(tied[1])).Group!.Role.Should().Be(MembershipRole.Member);
        (await Me(admin)).Group.Should().BeNull();
        await AssertHidden(admin, space.Id);

        (await Leave(tied[1], space.Id)).Should().Be(new LeaveSpaceResponse(false, false));
        (await Me(tied[0])).Group!.Role.Should().Be(MembershipRole.Admin);
        (await Leave(tied[0], space.Id)).Should().Be(new LeaveSpaceResponse(false, true));
        (await Me(latest)).Group!.Role.Should().Be(MembershipRole.Admin);
        (await Leave(latest, space.Id)).Should().Be(new LeaveSpaceResponse(true, false));

        using var join = await Send(outsider, HttpMethod.Post, "/api/groups/join", new { code });
        join.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await join.ErrorCodeAsync()).Should().Be("INVITE_INVALID");
        await using var finalDb = fixture.CreateContext();
        (await finalDb.Set<Group>().SingleAsync(g => g.Id == space.Id)).ClosedAt.Should().NotBeNull();
        (await finalDb.Set<InviteCode>().AnyAsync(i => i.GroupId == space.Id)).Should().BeFalse();
        (await finalDb.Set<GroupMembership>().AnyAsync(m => m.GroupId == space.Id)).Should().BeFalse();
        (await Me(latest)).Group.Should().BeNull();
    }

    [Fact]
    public async Task Leave_ConcurrentJoin_PreservesMembershipAndArchiveConsistency_FR10()
    {
        var admin = await _client.RegisterOkAsync();
        var joiner = await _client.RegisterOkAsync();
        var space = await Create(admin);
        var code = $"H{Guid.NewGuid():N}"[..16];
        var invite = TestData.NewInviteCode(space.Id, admin.User.Id, InviteCodeHasher.Hash(code));
        invite.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
        await fixture.SeedAsync(invite);

        var leaveTask = Leave(admin, space.Id);
        using var join = await Send(joiner, HttpMethod.Post, "/api/groups/join", new { code });
        var leave = await leaveTask;

        join.StatusCode.Should().Be(leave.Archived ? HttpStatusCode.NotFound : HttpStatusCode.OK);
        leave.AdminTransferred.Should().Be(!leave.Archived);
        (await Me(admin)).Group.Should().BeNull();
        var membership = (await Me(joiner)).Group;
        if (leave.Archived) membership.Should().BeNull();
        else membership!.Role.Should().Be(MembershipRole.Admin);
    }

    private async Task AssertHidden(AuthResponse auth, Guid id)
    {
        using var get = await Send(auth, HttpMethod.Get, $"/api/groups/{id}");
        using var leave = await Send(auth, HttpMethod.Post, $"/api/groups/{id}/leave");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        leave.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await get.ErrorCodeAsync()).Should().Be("SPACE_NOT_FOUND");
        (await leave.ErrorCodeAsync()).Should().Be("SPACE_NOT_FOUND");
    }

    private async Task<SpaceDetails> Create(AuthResponse auth)
    {
        using var response = await Send(auth, HttpMethod.Post, "/api/groups", new { name = "Test Alanı", type = "Ev", capacity = 5 });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var space = (await response.Content.ReadFromJsonAsync<SpaceDetails>(Json))!;
        using var detail = await Send(auth, HttpMethod.Get, $"/api/groups/{space.Id}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await detail.Content.ReadFromJsonAsync<SpaceDetails>(Json)).Should().Be(space);
        return space;
    }

    private async Task<LeaveSpaceResponse> Leave(AuthResponse auth, Guid id)
    {
        using var response = await Send(auth, HttpMethod.Post, $"/api/groups/{id}/leave");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LeaveSpaceResponse>(Json))!;
    }

    private async Task<MeResponse> Me(AuthResponse auth)
    {
        using var response = await _client.MeAsync(auth.AccessToken);
        return (await response.Content.ReadFromJsonAsync<MeResponse>(Json))!;
    }

    private async Task<HttpResponseMessage> Send(AuthResponse auth, HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new("Bearer", auth.AccessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }
}
