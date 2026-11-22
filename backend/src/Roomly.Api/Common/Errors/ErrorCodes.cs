namespace Roomly.Api.Common.Errors;

/// <summary>
/// Herkesin kullandığı ortak hata kodları. Modüle özel kodlar (ör. INVITE_EXPIRED)
/// kendi modül klasöründeki bir sabit sınıfta durur.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotFound = "NOT_FOUND";
    public const string Internal = "INTERNAL_ERROR";
}
