using Roomly.Api.Common.Results;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Auth.Dtos;
using Roomly.Api.Features.Groups.Dtos;

namespace Roomly.Api.Features.Groups;

/// <summary>
/// Davet koduyla katılım (spec 0006). Kurallar grup satırı kilitliyken tek transaction içinde
/// kontrol edilir (BND-01, TRG-01). Hata önceliği: INVITE_INVALID → ALREADY_MEMBER →
/// INVITE_REVOKED → INVITE_EXPIRED → ALREADY_IN_GROUP → CAPACITY_EXCEEDED.
/// </summary>
public sealed class JoinGroupService(IJoinGroupRepository repository, IClock clock, ILogger<JoinGroupService> logger)
{
    public async Task<Result<JoinGroupResponse>> JoinAsync(Guid userId, string code, CancellationToken ct)
    {
        var normalized = InviteCodeHasher.Normalize(code);
        if (!InviteCodeFormat.IsWellFormed(normalized))
        {
            return GroupErrors.InviteInvalid;
        }

        await using var transaction = await repository.BeginAsync(ct);

        var found = await repository.FindInviteAsync(InviteCodeHasher.Hash(normalized), ct);
        if (found is null)
        {
            return GroupErrors.InviteInvalid;
        }

        var group = await repository.LockGroupAsync(found.GroupId, ct);
        if (group is null || group.ClosedAt is not null)
        {
            return GroupErrors.InviteInvalid;
        }

        // Kilit alındıktan sonra tekrar oku: yarışan bir iptal ya da kapatma artık görünür.
        var invite = await repository.GetInviteAsync(found.Id, ct);
        if (invite is null)
        {
            return GroupErrors.InviteInvalid;
        }

        var currentGroupId = await repository.FindUserGroupIdAsync(userId, ct);
        if (currentGroupId == group.Id)
        {
            return GroupErrors.AlreadyMember;
        }

        if (invite.RevokedAt is not null)
        {
            return GroupErrors.InviteRevoked;
        }

        var now = clock.UtcNow;
        if (now > invite.ExpiresAt)
        {
            return GroupErrors.InviteExpired;
        }

        if (currentGroupId is not null)
        {
            return GroupErrors.AlreadyInGroup;
        }

        if (await repository.CountMembersAsync(group.Id, ct) >= group.Capacity)
        {
            return GroupErrors.CapacityExceeded;
        }

        var membership = new GroupMembership
        {
            GroupId = group.Id,
            UserId = userId,
            Role = MembershipRole.Member,
            JoinedAt = now,
        };

        switch (await repository.AddMembershipAsync(membership, ct))
        {
            case AddMembershipOutcome.AlreadyMember:
                return GroupErrors.AlreadyMember;
            case AddMembershipOutcome.UserInAnotherGroup:
                return GroupErrors.AlreadyInGroup;
        }

        await transaction.CommitAsync(ct);
        logger.LogInformation("Kullanıcı gruba katıldı: {UserId} {GroupId}", userId, group.Id);
        return new JoinGroupResponse(new GroupSummaryDto(group.Id, group.Name, group.Type, MembershipRole.Member));
    }
}
