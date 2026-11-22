using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Roomly.Api.Features.Auth.Dtos;

namespace Roomly.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Paylaşılan Postgres konteynerine bağlı API. Ortam <c>Testing</c> (demo seed yok), test JWT anahtarı.
/// Rate limit varsayılan olarak çok yüksek (bütün istekler aynı "IP"den gelir); SEC-04 testi gerçek limitle
/// ayrı bir örnek ister. Örnekler önbelleklenir, her test için host yeniden kurulmaz.
/// </summary>
public sealed class ApiFactory(string connectionString, int authPermitLimit) : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "integration-test-signing-key-0123456789abcdef-0123456789";

    private static readonly ConcurrentDictionary<(string, int), ApiFactory> Cache = new();

    public static CapturingLoggerProvider Logs { get; } = new();

    public static ApiFactory For(PostgresFixture fixture, int authPermitLimit = 100_000) =>
        Cache.GetOrAdd((fixture.ConnectionString, authPermitLimit), key => new ApiFactory(key.Item1, key.Item2));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        builder.UseSetting("RateLimiting:Auth:PermitLimit", authPermitLimit.ToString());
        builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }

    public T GetService<T>() where T : notnull => Services.GetRequiredService<T>();
}

internal static class AuthApi
{
    public const string ValidPassword = "Sifre1234";

    public static string UniqueEmail() => $"u{Guid.NewGuid():N}@test.local";

    public static Task<HttpResponseMessage> RegisterAsync(
        this HttpClient client, string? email = null, string password = ValidPassword, string displayName = "Test Kullanıcı") =>
        client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(displayName, email ?? UniqueEmail(), password));

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = ValidPassword) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(refreshToken));

    public static async Task<AuthResponse> RegisterOkAsync(this HttpClient client, string? email = null)
    {
        var response = await client.RegisterAsync(email);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static async Task<HttpResponseMessage> MeAsync(this HttpClient client, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    public static async Task<string?> ErrorCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return body.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}

/// <summary>Bütün log satırlarını bellekte toplar (SEC-07 testleri).</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _lines = new();

    public IReadOnlyCollection<string> Lines => _lines;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_lines);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> lines) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            lines.Enqueue(formatter(state, exception));
    }
}
