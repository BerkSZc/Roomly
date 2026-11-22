using System.Text;

namespace Roomly.Api.Features.Auth;

/// <summary><c>Jwt</c> yapılandırma bölümü (spec 0004).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// <c>appsettings.Development.json</c> içindeki açıkça sahte anahtar. Development dışında
    /// bu değerle uygulama açılmaz.
    /// </summary>
    public const string DevelopmentSigningKey =
        "roomly-dev-only-signing-key-do-not-use-in-production-0123456789abcdef";

    public const int MinKeyBytes = 32;

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "roomly-api";

    public string Audience { get; set; } = "roomly-mobile";

    public int AccessTokenMinutes { get; set; } = 15;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);

    public byte[] SigningKeyBytes => Encoding.UTF8.GetBytes(SigningKey);

    /// <summary>Anahtar eksik, kısa ya da (Development dışında) sahte varsayılansa açılışı durdurur.</summary>
    public void EnsureValid(bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKeyBytes.Length < MinKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey en az {MinKeyBytes} bayt olmalı. user-secrets ya da Jwt__SigningKey ile verin.");
        }

        if (!isDevelopment && SigningKey == DevelopmentSigningKey)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey geliştirme varsayılanı; Development dışında gerçek bir anahtar verin.");
        }
    }
}
