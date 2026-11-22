namespace Roomly.Api.Common.Logging;

/// <summary>
/// Loglara kişisel veri ve sır sızmasın diye maskeleme (SEC-07).
/// E-posta: <c>berk@ornek.com</c> → <c>b***@ornek.com</c>. Token: her zaman <c>***</c>.
/// </summary>
public static class LogMask
{
    public static string Email(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "(yok)";
        }

        var at = email.IndexOf('@');
        if (at <= 0)
        {
            return "***";
        }

        return $"{email[0]}***{email[at..]}";
    }

    public static string Token(string? token) => string.IsNullOrEmpty(token) ? "(yok)" : "***";
}
