using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Roomly.Api.Common.Errors;
using Roomly.Api.Common.Time;
using Roomly.Api.Infrastructure.Health;
using Roomly.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı: EF Core + Npgsql. EnableSensitiveDataLogging bilerek kapalı (SEC-07).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Default tanımlı değil. user-secrets ya da ortam değişkeni ile verin.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Ortak altyapı
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });
app.MapControllers();

app.Run();

// WebApplicationFactory<Program> için (entegrasyon testleri)
public partial class Program;
