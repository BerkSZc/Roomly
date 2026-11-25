using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Roomly.Api.Features.Groups.Dtos;
using Roomly.Api.Features.Groups.Validators;

namespace Roomly.Api.Features.Groups;

/// <summary><c>RateLimiting:Join</c> bölümü: kullanıcı başına sabit pencere (spec 0006 AC-10).</summary>
public sealed class JoinRateLimitOptions
{
    public const string SectionName = "RateLimiting:Join";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}

public static class GroupsServiceCollectionExtensions
{
    public const string JoinRateLimitPolicy = "join";

    public static IServiceCollection AddRoomlyGroups(this IServiceCollection services, IConfiguration configuration)
    {
        var join = configuration.GetSection(JoinRateLimitOptions.SectionName).Get<JoinRateLimitOptions>()
            ?? new JoinRateLimitOptions();

        // Reddetme yanıtı (429 RATE_LIMITED) auth kaydında ortak tanımlı.
        services.AddRateLimiter(limiter => limiter.AddPolicy(JoinRateLimitPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = join.PermitLimit,
                    Window = TimeSpan.FromSeconds(join.WindowSeconds),
                    QueueLimit = 0,
                })));

        services.AddScoped<IValidator<JoinGroupRequest>, JoinGroupRequestValidator>();
        services.AddScoped<IJoinGroupRepository, JoinGroupRepository>();
        services.AddScoped<JoinGroupService>();
        services.AddScoped<IValidator<CreateSpaceRequest>, CreateSpaceValidator>();
        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<SpaceService>();
        return services;
    }
}
