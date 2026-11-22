namespace Roomly.Api.Features.Groups;

/// <summary>
/// Davet kodu yalnızca <see cref="CodeHash"/> olarak durur; düz metin kod yanıtta bir kez döner.
/// Grupta en fazla bir iptal edilmemiş kod vardır: yeni kodda önce eskisi iptal edilir, sonra yenisi eklenir.
/// </summary>
public sealed class InviteCode
{
    public Guid Id { get; set; }

    public required Guid GroupId { get; set; }

    /// <summary><see cref="InviteCodeHasher.Hash"/> sonucu, 32 bayt.</summary>
    public required byte[] CodeHash { get; set; }

    public required Guid CreatedByUserId { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary><c>CreatedAt + InviteCodeRules.Lifetime</c>.</summary>
    public required DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
