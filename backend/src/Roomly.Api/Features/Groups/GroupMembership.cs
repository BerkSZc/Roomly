namespace Roomly.Api.Features.Groups;

/// <summary>
/// Kullanıcının gruptaki üyeliği. Kullanıcı en fazla bir gruptadır, grupta en fazla bir yönetici vardır.
/// Yönetici devrinde önce eski yönetici bırakılır, sonra yenisi yazılır (Groups README, yazma sırası).
/// </summary>
public sealed class GroupMembership
{
    public required Guid GroupId { get; set; }

    public required Guid UserId { get; set; }

    public required MembershipRole Role { get; set; }

    public required DateTimeOffset JoinedAt { get; set; }
}
