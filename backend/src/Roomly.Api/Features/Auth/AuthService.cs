using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Roomly.Api.Common.Logging;
using Roomly.Api.Common.Results;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Auth.Dtos;

namespace Roomly.Api.Features.Auth;

/// <summary>Kayıt, giriş (kilit), refresh rotasyonu, çıkış ve <c>me</c> (spec 0004).</summary>
public sealed class AuthService(
    UserManager<AppUser> users,
    IAuthRepository repository,
    IAccessTokenIssuer accessTokens,
    IClock clock,
    ILogger<AuthService> logger)
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email!);
        if (await users.FindByEmailAsync(email) is not null)
        {
            logger.LogInformation("Kayıt reddedildi, e-posta kayıtlı: {Email}", LogMask.Email(email));
            return AuthErrors.EmailTaken;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName!.Trim(),
            Email = email,
            UserName = email,
            CreatedAt = clock.UtcNow,
        };

        IdentityResult created;
        try
        {
            created = await users.CreateAsync(user, request.Password!);
        }
        catch (DbUpdateException ex) when (IsEmailUniqueViolation(ex))
        {
            return AuthErrors.EmailTaken;
        }

        if (!created.Succeeded)
        {
            if (created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return AuthErrors.EmailTaken;
            }

            throw new InvalidOperationException(
                "Kullanıcı oluşturulamadı: " + string.Join(", ", created.Errors.Select(e => e.Code)));
        }

        logger.LogInformation("Yeni kullanıcı kaydoldu: {UserId} {Email}", user.Id, LogMask.Email(email));
        return await IssueTokensAsync(user, Guid.NewGuid(), ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email!);
        var user = await users.FindByEmailAsync(email);
        if (user is null || user.DeletedAt is not null)
        {
            logger.LogInformation("Giriş reddedildi, hesap yok: {Email}", LogMask.Email(email));
            return AuthErrors.InvalidCredentials;
        }

        if (await users.IsLockedOutAsync(user))
        {
            logger.LogWarning("Giriş reddedildi, hesap kilitli: {UserId}", user.Id);
            return AuthErrors.AccountLocked;
        }

        if (!await users.CheckPasswordAsync(user, request.Password!))
        {
            await users.AccessFailedAsync(user);
            if (await users.IsLockedOutAsync(user))
            {
                logger.LogWarning("Hesap kilitlendi (SEC-01): {UserId}", user.Id);
                return AuthErrors.AccountLocked;
            }

            logger.LogInformation("Giriş reddedildi, şifre hatalı: {UserId}", user.Id);
            return AuthErrors.InvalidCredentials;
        }

        if (user.AccessFailedCount > 0)
        {
            await users.ResetAccessFailedCountAsync(user);
        }

        logger.LogInformation("Giriş başarılı: {UserId}", user.Id);
        return await IssueTokensAsync(user, Guid.NewGuid(), ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var current = await repository.FindRefreshTokenAsync(RefreshTokenGenerator.Hash(request.RefreshToken!), ct);
        if (current is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        if (current.RevokedAt is not null)
        {
            await repository.RevokeFamilyAsync(current.FamilyId, now, ct);
            logger.LogWarning("Refresh token yeniden kullanıldı, aile iptal edildi: {UserId}", current.UserId);
            return AuthErrors.InvalidRefreshToken;
        }

        if (now > current.ExpiresAt)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var user = await users.FindByIdAsync(current.UserId.ToString());
        if (user is null || user.DeletedAt is not null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var generated = RefreshTokenGenerator.Generate();
        var replacement = NewRefreshToken(user.Id, current.FamilyId, generated.Hash, now);
        if (!await repository.TryRotateAsync(current.Id, replacement, now, ct))
        {
            await repository.RevokeFamilyAsync(current.FamilyId, now, ct);
            logger.LogWarning("Refresh token yarışı ya da yeniden kullanımı, aile iptal edildi: {UserId}", user.Id);
            return AuthErrors.InvalidRefreshToken;
        }

        return BuildResponse(user, generated.PlainText, replacement.ExpiresAt);
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken ct)
    {
        var token = await repository.FindRefreshTokenAsync(RefreshTokenGenerator.Hash(request.RefreshToken!), ct);
        if (token is not null)
        {
            await repository.RevokeFamilyAsync(token.FamilyId, clock.UtcNow, ct);
            logger.LogInformation("Çıkış yapıldı: {UserId}", token.UserId);
        }
    }

    public async Task<Result<MeResponse>> MeAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || user.DeletedAt is not null)
        {
            return Error.NotFound("Kullanıcı bulunamadı.");
        }

        var group = await repository.FindOpenGroupAsync(userId, ct);
        return new MeResponse(user.Id, user.DisplayName, user.Email!, group);
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, Guid familyId, CancellationToken ct)
    {
        var generated = RefreshTokenGenerator.Generate();
        var token = NewRefreshToken(user.Id, familyId, generated.Hash, clock.UtcNow);
        await repository.AddRefreshTokenAsync(token, ct);
        return BuildResponse(user, generated.PlainText, token.ExpiresAt);
    }

    private AuthResponse BuildResponse(AppUser user, string refreshToken, DateTimeOffset refreshExpiresAt)
    {
        var access = accessTokens.Issue(user.Id);
        return new AuthResponse(
            access.Value,
            access.ExpiresAt,
            refreshToken,
            refreshExpiresAt,
            new UserDto(user.Id, user.DisplayName, user.Email!));
    }

    private static RefreshToken NewRefreshToken(Guid userId, Guid familyId, byte[] hash, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        FamilyId = familyId,
        TokenHash = hash,
        CreatedAt = now,
        ExpiresAt = now + RefreshTokenRules.Lifetime,
    };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsEmailUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && pg.ConstraintName is AuthDbNames.UsersNormalizedEmailUnique or AuthDbNames.UsersNormalizedUserNameUnique;
}
