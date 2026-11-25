using Roomly.Api.Common.Results;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Groups.Dtos;

namespace Roomly.Api.Features.Groups;

public sealed class SpaceService(ISpaceRepository spaces, IJoinGroupRepository groups, IClock clock)
{
    public async Task<Result<SpaceDetails>> CreateAsync(
        Guid userId, string name, GroupType type, int capacity, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var group = new Group { Id = Guid.NewGuid(), Name = name.Trim(), Type = type, Capacity = capacity, CreatedAt = now };
        var founder = new GroupMembership
        {
            GroupId = group.Id, UserId = userId, Role = MembershipRole.Admin, JoinedAt = now,
        };

        if (!await spaces.CreateAsync(group, founder, ct))
        {
            return GroupErrors.AlreadyInGroup;
        }

        return new SpaceDetails(group.Id, group.Name, group.Type, founder.Role, group.Capacity, 1);
    }

    public async Task<Result<SpaceDetails>> GetAsync(Guid groupId, Guid userId, CancellationToken ct)
    {
        var space = await spaces.GetAsync(groupId, userId, ct);
        return space is null ? GroupErrors.SpaceNotFound : space;
    }

    public async Task<Result<LeaveSpaceResponse>> LeaveAsync(Guid groupId, Guid userId, CancellationToken ct)
    {
        // Grup dışından gelen istekler grup satırını kilitlemez.
        if (await spaces.GetRoleAsync(groupId, userId, ct) is null)
        {
            return GroupErrors.SpaceNotFound;
        }

        await using var transaction = await groups.BeginAsync(ct);
        var group = await groups.LockGroupAsync(groupId, ct);
        if (group is null || group.ClosedAt is not null)
        {
            return GroupErrors.SpaceNotFound;
        }

        var role = await spaces.GetRoleAsync(groupId, userId, ct);
        if (role is null)
        {
            return GroupErrors.SpaceNotFound;
        }

        // Ayrılan yönetici satırı, yeni yönetici yazılmadan önce veritabanından kaldırılır.
        await spaces.RemoveAsync(groupId, userId, ct);
        var oldest = await spaces.FindOldestAsync(groupId, ct);
        var archived = oldest is null;
        var transferred = false;
        if (oldest is null)
        {
            await spaces.ArchiveAsync(groupId, clock.UtcNow, ct);
        }
        else if (role == MembershipRole.Admin)
        {
            await spaces.PromoteAsync(oldest, ct);
            transferred = true;
        }

        await transaction.CommitAsync(ct);
        return new LeaveSpaceResponse(archived, transferred);
    }
}
