using System.Text.RegularExpressions;

namespace Roomly.Api.Features.Groups;

/// <summary>
/// Normalize edilmiş davet kodunun biçimi: 8 ile 32 arası <c>A-Z0-9</c> (spec 0006 AC-2).
/// Üretilen kodlar (scope #10) bu desenin içinde kalır; demo kodu <c>DEMOROOM2026</c> da uyar.
/// </summary>
public static partial class InviteCodeFormat
{
    public const int MinLength = 8;
    public const int MaxLength = 32;

    public static bool IsWellFormed(string normalizedCode) => Pattern().IsMatch(normalizedCode);

    [GeneratedRegex("^[A-Z0-9]{8,32}$")]
    private static partial Regex Pattern();
}
