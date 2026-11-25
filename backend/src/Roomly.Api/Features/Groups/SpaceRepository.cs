using Microsoft.EntityFrameworkCore;
using Npgsql;
using Roomly.Api.Features.Groups.Dtos;
using Roomly.Api.Infrastructure.Persistence;

namespace Roomly.Api.Features.Groups;

public interface ISpaceRepository
{
    Task<bool> CreateAsync(Group group, GroupMembership founder, CancellationToken ct);
    Task<SpaceDetails?> GetAsync(Guid groupId, Guid userId, CancellationToken ct);
    Task<MembershipRole?> GetRoleAsync(Guid groupId, Guid userId, CancellationToken ct);
    Task RemoveAsync(Guid groupId, Guid userId, CancellationToken ct);
    Task<GroupMembership?> FindOldestAsync(Guid groupId, CancellationToken ct);
    Task PromoteAsync(GroupMembership member, CancellationToken ct);
    Task ArchiveAsync(Guid groupId, DateTimeOffset now, CancellationToken ct);
}

public sealed class SpaceRepository(AppDbContext db) : ISpaceRepository
{
    public async Task<bool> CreateAsync(Group group, GroupMembership founder, CancellationToken ct)
    {
        db.AddRange(group, founder);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: GroupsDbNames.MembershipsUserIdUnique,
        })
        {
            db.Entry(founder).State = EntityState.Detached;
            db.Entry(group).State = EntityState.Detached;
            return false;
        }
    }

    private IQueryable<GroupMembership> Members(Guid groupId) =>
        db.Set<GroupMembership>().Where(m => m.GroupId == groupId);

    public Task<SpaceDetails?> GetAsync(Guid groupId, Guid userId, CancellationToken ct) =>
        (from member in Members(groupId)
         join space in db.Set<Group>() on member.GroupId equals space.Id
         where member.UserId == userId && space.ClosedAt == null
         select new SpaceDetails(space.Id, space.Name, space.Type, member.Role, space.Capacity,
             db.Set<GroupMembership>().Count(other => other.GroupId == space.Id)))
        .SingleOrDefaultAsync(ct);

    public Task<MembershipRole?> GetRoleAsync(Guid groupId, Guid userId, CancellationToken ct) =>
        Members(groupId).Where(m => m.UserId == userId)
            .Select(m => (MembershipRole?)m.Role).SingleOrDefaultAsync(ct);

    public Task RemoveAsync(Guid groupId, Guid userId, CancellationToken ct) =>
        Members(groupId).Where(m => m.UserId == userId).ExecuteDeleteAsync(ct);

    public Task<GroupMembership?> FindOldestAsync(Guid groupId, CancellationToken ct) =>
        Members(groupId).OrderBy(m => m.JoinedAt).ThenBy(m => m.UserId).FirstOrDefaultAsync(ct);

    public async Task PromoteAsync(GroupMembership member, CancellationToken ct)
    {
        member.Role = MembershipRole.Admin;
        await db.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid groupId, DateTimeOffset now, CancellationToken ct)
    {
        await db.Set<InviteCode>().Where(i => i.GroupId == groupId).ExecuteDeleteAsync(ct);
        await db.Set<Group>().Where(g => g.Id == groupId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(g => g.ClosedAt, now), ct);
    }
}
