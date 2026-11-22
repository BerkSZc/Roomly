using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Roomly.Api.Infrastructure.Health;

/// <summary><c>GET /health</c> yanıtı: <c>{ "status": "Healthy", "checks": { "database": "Healthy" } }</c></summary>
internal static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        });
}
