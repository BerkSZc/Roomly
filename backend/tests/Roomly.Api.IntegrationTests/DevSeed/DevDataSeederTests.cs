using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Groups;
using Roomly.Api.Infrastructure.Persistence;
using Roomly.Api.IntegrationTests.Infrastructure;

namespace Roomly.Api.IntegrationTests.DevSeed;

/// <summary>Development seed'i (scope #5). Her test kendi e-posta ve koduyla, paylaşılan DB'ye dokunmadan.</summary>
[Collection(PostgresCollection.Name)]
public sealed class DevDataSeederTests(PostgresFixture fixture)
{
    private readonly ApiFactory _factory = ApiFactory.For(fixture);

    private static DevSeedOptions NewOptions() => new()
    {
        AdminEmail = $"demo{Guid.NewGuid():N}@test.local",
        InviteCode = "S" + Convert.ToHexString(Guid.NewGuid().ToByteArray())[..11],
    };

    private async Task SeedAsync(DevSeedOptions options)
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var seeder = new DevDataSeeder(
            services.GetRequiredService<AppDbContext>(),
            services.GetRequiredService<UserManager<AppUser>>(),
            services.GetRequiredService<IClock>(),
            Options.Create(options),
            NullLogger<DevDataSeeder>.Instance);
        await seeder.SeedAsync();
    }

    [Fact]
    public async Task Seed_RunTwice_CreatesAdminGroupAndCodeOnlyOnce_SEED01()
    {
        var options = NewOptions();

        await SeedAsync(options);
        await SeedAsync(options);

        await using var db = fixture.CreateContext();
        var admin = await db.Set<AppUser>().SingleAsync(u => u.Email == options.AdminEmail);
        var membership = await db.Set<GroupMembership>().SingleAsync(m => m.UserId == admin.Id);
        membership.Role.Should().Be(MembershipRole.Admin);
        var group = await db.Set<Group>().SingleAsync(g => g.Id == membership.GroupId);
        group.Name.Should().Be("Demo Ev");
        group.Type.Should().Be(GroupType.Ev);
        group.Capacity.Should().Be(6);
        var codes = await db.Set<InviteCode>().Where(i => i.GroupId == group.Id).ToListAsync();
        codes.Should().ContainSingle();
        codes[0].CodeHash.Should().Equal(InviteCodeHasher.Hash(options.InviteCode));
        codes[0].RevokedAt.Should().BeNull();
        codes[0].ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddYears(5));
    }

    [Fact]
    public async Task Seed_AdminCanLogInAndNewUserCanJoinWithSeededCode_SEED02()
    {
        var options = NewOptions();
        await SeedAsync(options);
        var client = _factory.CreateClient();

        (await client.LoginAsync(options.AdminEmail, options.AdminPassword)).EnsureSuccessStatusCode();

        var joiner = TestData.NewUser();
        await fixture.SeedAsync(joiner);
        await using var db = fixture.CreateContext();
        var join = new JoinGroupService(
            new JoinGroupRepository(db), _factory.GetService<IClock>(), NullLogger<JoinGroupService>.Instance);
        var result = await join.JoinAsync(joiner.Id, options.InviteCode.ToLowerInvariant(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Group.Name.Should().Be("Demo Ev");
    }

    [Fact]
    public async Task Seed_WithMalformedInviteCode_Throws_SEED01()
    {
        var options = NewOptions();
        options.InviteCode = "kısa";

        await FluentActions.Invoking(() => SeedAsync(options)).Should().ThrowAsync<InvalidOperationException>();
    }
}
