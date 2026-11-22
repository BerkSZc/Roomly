using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Roomly.Api.Common.Logging;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Groups;

namespace Roomly.Api.Infrastructure.Persistence;

/// <summary>
/// <c>DevSeed</c> bölümü (yalnızca <c>appsettings.Development.json</c>). Değerler bilerek zayıf ve
/// herkese açık; seed yalnızca Development ortamında çalışır (spec 0001, scope #5).
/// </summary>
public sealed class DevSeedOptions
{
    public const string SectionName = "DevSeed";

    public string AdminEmail { get; set; } = "demo@roomly.app";

    public string AdminPassword { get; set; } = "Demo1234";

    public string AdminDisplayName { get; set; } = "Demo Kullanıcı";

    public string GroupName { get; set; } = "Demo Ev";

    public GroupType GroupType { get; set; } = GroupType.Ev;

    public int GroupCapacity { get; set; } = 6;

    public string InviteCode { get; set; } = "DEMO-ROOM-2026";

    /// <summary>Demo kodu her açılışta yeniden üretilmesin diye uzak gelecekte biter (spec 0002 follow-up).</summary>
    public int InviteCodeLifetimeDays { get; set; } = 3650;
}

/// <summary>
/// Demo yönetici, onun yönettiği demo grup ve demo davet kodu. Tekrar çalıştırılabilir: var olanı bulur,
/// eksik olanı ekler, kopya ya da kısıt hatası üretmez.
/// </summary>
public sealed class DevDataSeeder(
    AppDbContext db,
    UserManager<AppUser> users,
    IClock clock,
    IOptions<DevSeedOptions> options,
    ILogger<DevDataSeeder> logger)
{
    private readonly DevSeedOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!InviteCodeFormat.IsWellFormed(InviteCodeHasher.Normalize(_options.InviteCode)))
        {
            throw new InvalidOperationException("DevSeed:InviteCode davet kodu biçimine uymuyor.");
        }

        var admin = await EnsureAdminAsync();
        var group = await EnsureGroupAsync(admin, ct);
        if (group is not null)
        {
            await EnsureInviteCodeAsync(group, admin, ct);
        }
    }

    private async Task<AppUser> EnsureAdminAsync()
    {
        var email = _options.AdminEmail.Trim().ToLowerInvariant();
        var admin = await users.FindByEmailAsync(email);
        if (admin is not null)
        {
            return admin;
        }

        admin = new AppUser
        {
            Id = Guid.NewGuid(),
            DisplayName = _options.AdminDisplayName,
            Email = email,
            UserName = email,
            CreatedAt = clock.UtcNow,
        };
        var created = await users.CreateAsync(admin, _options.AdminPassword);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                "Demo kullanıcı oluşturulamadı: " + string.Join(", ", created.Errors.Select(e => e.Code)));
        }

        logger.LogInformation("Demo kullanıcı oluşturuldu: {Email}", LogMask.Email(email));
        return admin;
    }

    private async Task<Group?> EnsureGroupAsync(AppUser admin, CancellationToken ct)
    {
        var membership = await db.Set<GroupMembership>().AsNoTracking()
            .SingleOrDefaultAsync(m => m.UserId == admin.Id, ct);
        if (membership is not null)
        {
            var existing = await db.Set<Group>().AsNoTracking().SingleAsync(g => g.Id == membership.GroupId, ct);
            if (membership.Role == MembershipRole.Admin && existing.ClosedAt is null)
            {
                return existing;
            }

            logger.LogWarning("Demo kullanıcı başka bir grupta üye; demo grup ve kod atlandı.");
            return null;
        }

        var now = clock.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = _options.GroupName,
            Type = _options.GroupType,
            Capacity = _options.GroupCapacity,
            CreatedAt = now,
        };

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Set<Group>().Add(group);
        db.Set<GroupMembership>().Add(new GroupMembership
        {
            GroupId = group.Id,
            UserId = admin.Id,
            Role = MembershipRole.Admin,
            JoinedAt = now,
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation("Demo grup oluşturuldu: {GroupId}", group.Id);
        return group;
    }

    private async Task EnsureInviteCodeAsync(Group group, AppUser admin, CancellationToken ct)
    {
        var hash = InviteCodeHasher.Hash(_options.InviteCode);
        var existing = await db.Set<InviteCode>().AsNoTracking().SingleOrDefaultAsync(i => i.CodeHash == hash, ct);
        var now = clock.UtcNow;
        if (existing is not null)
        {
            if (existing.GroupId != group.Id || existing.RevokedAt is not null || now > existing.ExpiresAt)
            {
                logger.LogWarning("Demo davet kodu kullanılamaz durumda (başka grup, iptal ya da süresi dolmuş).");
            }

            return;
        }

        // Yazma sırası (spec 0002): grubu kilitle, önce eski aktif kodu iptal et, sonra yenisini ekle.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Set<Group>()
            .FromSql($"SELECT * FROM groups WHERE id = {group.Id} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(ct);
        await db.Set<InviteCode>()
            .Where(i => i.GroupId == group.Id && i.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.RevokedAt, now), ct);
        db.Set<InviteCode>().Add(new InviteCode
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            CodeHash = hash,
            CreatedByUserId = admin.Id,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.InviteCodeLifetimeDays),
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation("Demo davet kodu eklendi: {GroupId}", group.Id);
    }
}
