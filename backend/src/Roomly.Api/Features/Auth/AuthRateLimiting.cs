using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Roomly.Api.Common.Errors;

namespace Roomly.Api.Features.Auth;

/// <summary><c>RateLimiting:Auth</c> bölümü: IP başına sabit pencere (SEC-04).</summary>
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}

public static class AuthRateLimiting
{
    public const string PolicyName = "auth";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>()
            ?? new AuthRateLimitOptions();

        return services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(PolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit = 0,
                    }));

            limiter.OnRejected = async (context, ct) =>
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Çok fazla istek gönderildi. Lütfen biraz bekleyip tekrar deneyin.",
                };
                problem.Extensions["code"] = ErrorCodes.RateLimited;
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", ct);
            };
        });
    }
}
