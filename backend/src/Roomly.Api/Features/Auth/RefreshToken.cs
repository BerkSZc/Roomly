namespace Roomly.Api.Features.Auth;

/// <summary>
/// Refresh token yalnızca özet olarak durur. Rotasyonda eski satıra <see cref="RevokedAt"/> ve
/// <see cref="ReplacedById"/> yazılır, yeni satır aynı <see cref="FamilyId"/>'yi devralır.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required byte[] TokenHash { get; set; }

    public required Guid FamilyId { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedById { get; set; }
}
