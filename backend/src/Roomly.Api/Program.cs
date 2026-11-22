using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Roomly.Api.Common.Errors;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Groups;
using Roomly.Api.Infrastructure.Health;
using Roomly.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı: EF Core + Npgsql, snake_case adlar (spec 0002). EnableSensitiveDataLogging bilerek kapalı (SEC-07).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Default tanımlı değil. user-secrets ya da ortam değişkeni ile verin.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseRoomlyNpgsql(connectionString));

// Ortak altyapı
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Kimlik doğrulama (spec 0004): Identity + JWT bearer, varsayılan olarak korumalı uçlar, auth rate limit.
builder.Services.AddRoomlyAuth(builder.Configuration, builder.Environment);

// Gruplar: davet koduyla katılım (spec 0006).
builder.Services.AddRoomlyGroups(builder.Configuration);

// Development seed: demo kullanıcı, demo grup, DEMO-ROOM-2026 (yalnızca Development'ta çalışır).
builder.Services.AddDevDataSeeding(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Model bağlama hataları tek tip: 400 ProblemDetails + code: VALIDATION_FAILED
builder.Services.Configure<ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = ValidationProblems.FromModelState);

builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Roomly API", Version = "v1" });
    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Giriş yanıtındaki access token'ı yapıştırın.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    c.AddSecurityDefinition("Bearer", bearer);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = Array.Empty<string>() });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    await app.InitializeDevelopmentDatabaseAsync();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync })
    .AllowAnonymous();
app.MapControllers();

await app.RunAsync();

// WebApplicationFactory<Program> için (entegrasyon testleri)
public partial class Program;
