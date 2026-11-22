using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Roomly.Api.Common.Errors;
using Roomly.Api.Features.Auth.Validators;
using Roomly.Api.Infrastructure.Persistence;

namespace Roomly.Api.Features.Auth;

public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Identity (çerezsiz), JWT bearer, varsayılan olarak korumalı uçlar, auth rate limit ve modül servisleri.
    /// </summary>
    public static IServiceCollection AddRoomlyAuth(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        jwt.EnsureValid(environment.IsDevelopment());
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services
            .AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = AuthRules.PasswordMin;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 1;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.SigningKeyBytes),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                };
                o.Events = new JwtBearerEvents { OnChallenge = WriteUnauthorizedAsync };
            });

        // Ekibin bütün uçları varsayılan olarak oturum ister; anonim uç [AllowAnonymous] ile açılır.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddAuthRateLimiting(configuration);

        services.AddScoped<IValidator<Dtos.RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<Dtos.LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<Dtos.RefreshRequest>, RefreshRequestValidator>();
        services.AddScoped<IAccessTokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<AuthService>();
        return services;
    }

    private static async Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Oturum açmanız gerekiyor.",
        };
        problem.Extensions["code"] = ErrorCodes.Unauthorized;
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(
            problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
