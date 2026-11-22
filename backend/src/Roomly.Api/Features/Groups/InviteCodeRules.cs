namespace Roomly.Api.Features.Groups;

public static class InviteCodeRules
{
    /// <summary><c>expires_at = created_at + Lifetime</c>. Tam 48. saatte kod hâlâ geçerlidir.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(48);

    /// <summary>SHA-256 özetinin bayt uzunluğu.</summary>
    public const int HashLength = 32;
}
