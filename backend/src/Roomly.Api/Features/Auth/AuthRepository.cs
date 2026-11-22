using Microsoft.EntityFrameworkCore;
using Roomly.Api.Features.Auth.Dtos;
using Roomly.Api.Features.Groups;
using Roomly.Api.Infrastructure.Persistence;

namespace Roomly.Api.Features.Auth;

public interface IAuthRepository
{
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct);

    Task<RefreshToken?> FindRefreshTokenAsync(byte[] tokenHash, CancellationToken ct);

    /// <summary>
    /// Tek transaction: eski token'ı koşullu iptal et, yenisini ekle, eskisine <c>replaced_by_id</c> yaz.
    /// Eski token zaten iptalse (yarış ya da yeniden kullanım) hiçbir şey yazmaz ve <c>false</c> döner.
    /// </summary>
    Task<bool> TryRotateAsync(Guid oldTokenId, RefreshToken replacement, DateTimeOffset now, CancellationToken ct);

    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken ct);

    Task<GroupSummaryDto?> FindOpenGroupAsync(Guid userId, CancellationToken ct);
}

public sealed class AuthRepository(AppDbContext db) : IAuthRepository
{
    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct)
    {
        db.Set<RefreshToken>().Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> FindRefreshTokenAsync(byte[] tokenHash, CancellationToken ct) =>
        db.Set<RefreshToken>().AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<bool> TryRotateAsync(
        Guid oldTokenId, RefreshToken replacement, DateTimeOffset now, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var revoked = await db.Set<RefreshToken>()
            .Where(t => t.Id == oldTokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        if (revoked != 1)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        db.Set<RefreshToken>().Add(replacement);
        await db.SaveChangesAsync(ct);

        await db.Set<RefreshToken>()
            .Where(t => t.Id == oldTokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ReplacedById, replacement.Id), ct);

        await tx.CommitAsync(ct);
        return true;
    }

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken ct) =>
        db.Set<RefreshToken>()
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

    public Task<GroupSummaryDto?> FindOpenGroupAsync(Guid userId, CancellationToken ct) =>
        (from m in db.Set<GroupMembership>()
         join g in db.Set<Group>() on m.GroupId equals g.Id
         where m.UserId == userId && g.ClosedAt == null
         select new GroupSummaryDto(g.Id, g.Name, g.Type, m.Role))
        .AsNoTracking()
        .SingleOrDefaultAsync(ct);
}
