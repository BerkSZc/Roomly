using Roomly.Api.Common.Results;

namespace Roomly.Api.Features.Auth;

/// <summary>Auth modülünün hata kodları (spec 0004).</summary>
public static class AuthErrors
{
    public const string EmailTakenCode = "EMAIL_TAKEN";
    public const string InvalidCredentialsCode = "INVALID_CREDENTIALS";
    public const string AccountLockedCode = "ACCOUNT_LOCKED";
    public const string InvalidRefreshTokenCode = "INVALID_REFRESH_TOKEN";

    public static readonly Error EmailTaken = Error.Conflict(
        EmailTakenCode, "Bu e-posta adresiyle kayıtlı bir hesap var.");

    public static readonly Error InvalidCredentials = new(
        InvalidCredentialsCode, "E-posta ya da şifre hatalı.", StatusCodes.Status401Unauthorized);

    public static readonly Error AccountLocked = new(
        AccountLockedCode,
        "Çok fazla hatalı deneme yapıldı. Hesabınız 15 dakika kilitlendi.",
        StatusCodes.Status423Locked);

    public static readonly Error InvalidRefreshToken = new(
        InvalidRefreshTokenCode, "Oturumun süresi doldu. Lütfen tekrar giriş yapın.",
        StatusCodes.Status401Unauthorized);
}
