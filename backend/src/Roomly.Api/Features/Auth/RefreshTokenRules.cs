namespace Roomly.Api.Features.Auth;

public static class RefreshTokenRules
{
    /// <summary><c>expires_at = created_at + Lifetime</c>.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    /// <summary>SHA-256 özetinin bayt uzunluğu.</summary>
    public const int HashLength = 32;
}
