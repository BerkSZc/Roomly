using System.Security.Cryptography;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Groups;

namespace Roomly.Api.IntegrationTests.Infrastructure;

/// <summary>Her çağrıda benzersiz kimlik ve e-postayla geçerli satırlar üretir.</summary>
internal static class TestData
{
    /// <summary>Sabit test zamanı (offset 0); uygulamada bu değer <c>IClock.UtcNow</c>'dan gelir.</summary>
    public static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    public static AppUser NewUser(string displayName = "Test Kullanıcı", string? email = null)
    {
        email ??= $"u{Guid.NewGuid():N}@test.local";
        return new AppUser
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = Now,
        };
    }

    public static Group NewGroup(GroupType type = GroupType.Ev, int capacity = 5) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Evi",
        Type = type,
        Capacity = capacity,
        CreatedAt = Now,
    };

    public static GroupMembership NewMembership(
        Guid groupId, Guid userId, MembershipRole role = MembershipRole.Member) => new()
        {
            GroupId = groupId,
            UserId = userId,
            Role = role,
            JoinedAt = Now,
        };

    public static InviteCode NewInviteCode(
        Guid groupId, Guid createdByUserId, byte[]? codeHash = null, DateTimeOffset? revokedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            CodeHash = codeHash ?? RandomHash(),
            CreatedByUserId = createdByUserId,
            CreatedAt = Now,
            ExpiresAt = Now + InviteCodeRules.Lifetime,
            RevokedAt = revokedAt,
        };

    public static RefreshToken NewRefreshToken(
        Guid userId, Guid? familyId = null, byte[]? tokenHash = null, Guid? replacedById = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash ?? RandomHash(),
            FamilyId = familyId ?? Guid.NewGuid(),
            CreatedAt = Now,
            ExpiresAt = Now + RefreshTokenRules.Lifetime,
            ReplacedById = replacedById,
        };

    public static byte[] RandomHash(int length = 32) => RandomNumberGenerator.GetBytes(length);
}
