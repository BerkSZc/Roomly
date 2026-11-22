using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Roomly.Api.Infrastructure.Persistence;

namespace Roomly.Api.Features.Groups;

public sealed record InviteLookup(Guid Id, Guid GroupId, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);

public enum AddMembershipOutcome
{
    Added,
    AlreadyMember,
    UserInAnotherGroup,
}

/// <summary>Katılımın veritabanı adımları; hepsi <see cref="BeginAsync"/> transaction'ı içinde çağrılır.</summary>
public interface IJoinGroupRepository
{
    Task<IDbContextTransaction> BeginAsync(CancellationToken ct);

    Task<InviteLookup?> FindInviteAsync(byte[] codeHash, CancellationToken ct);

    Task<InviteLookup?> GetInviteAsync(Guid inviteId, CancellationToken ct);

    /// <summary><c>SELECT ... FOR UPDATE</c>: transaction bitene kadar gruba yazan herkes bekler.</summary>
    Task<Group?> LockGroupAsync(Guid groupId, CancellationToken ct);

    Task<Guid?> FindUserGroupIdAsync(Guid userId, CancellationToken ct);

    Task<int> CountMembersAsync(Guid groupId, CancellationToken ct);

    Task<AddMembershipOutcome> AddMembershipAsync(GroupMembership membership, CancellationToken ct);
}

public sealed class JoinGroupRepository(AppDbContext db) : IJoinGroupRepository
{
    public Task<IDbContextTransaction> BeginAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);

    public Task<InviteLookup?> FindInviteAsync(byte[] codeHash, CancellationToken ct) =>
        db.Set<InviteCode>().AsNoTracking()
            .Where(i => i.CodeHash == codeHash)
            .Select(i => new InviteLookup(i.Id, i.GroupId, i.ExpiresAt, i.RevokedAt))
            .SingleOrDefaultAsync(ct);

    public Task<InviteLookup?> GetInviteAsync(Guid inviteId, CancellationToken ct) =>
        db.Set<InviteCode>().AsNoTracking()
            .Where(i => i.Id == inviteId)
            .Select(i => new InviteLookup(i.Id, i.GroupId, i.ExpiresAt, i.RevokedAt))
            .SingleOrDefaultAsync(ct);

    public async Task<Group?> LockGroupAsync(Guid groupId, CancellationToken ct)
    {
        // ToListAsync: EF sorguyu alt sorguya sarmadan olduğu gibi gönderir.
        var rows = await db.Set<Group>()
            .FromSql($"SELECT * FROM groups WHERE id = {groupId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(ct);
        return rows.SingleOrDefault();
    }

    public Task<Guid?> FindUserGroupIdAsync(Guid userId, CancellationToken ct) =>
        db.Set<GroupMembership>().AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => (Guid?)m.GroupId)
            .SingleOrDefaultAsync(ct);

    public Task<int> CountMembersAsync(Guid groupId, CancellationToken ct) =>
        db.Set<GroupMembership>().CountAsync(m => m.GroupId == groupId, ct);

    public async Task<AddMembershipOutcome> AddMembershipAsync(GroupMembership membership, CancellationToken ct)
    {
        db.Set<GroupMembership>().Add(membership);
        try
        {
            await db.SaveChangesAsync(ct);
            return AddMembershipOutcome.Added;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        } pg)
        {
            db.Entry(membership).State = EntityState.Detached;
            return pg.ConstraintName switch
            {
                GroupsDbNames.MembershipsPrimaryKey => AddMembershipOutcome.AlreadyMember,
                GroupsDbNames.MembershipsUserIdUnique => AddMembershipOutcome.UserInAnotherGroup,
                _ => throw new InvalidOperationException($"Beklenmeyen kısıt: {pg.ConstraintName}", ex),
            };
        }
    }
}
