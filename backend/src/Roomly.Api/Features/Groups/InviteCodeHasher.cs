using System.Security.Cryptography;
using System.Text;

namespace Roomly.Api.Features.Groups;

/// <summary>
/// Davet kodu düz metin saklanmaz; yalnızca <see cref="Hash"/> sonucu durur. Üretim ve katılım
/// aynı normalizasyonu kullanır, böylece <c>DEMO-ROOM-2026</c> ile <c>demo room 2026</c> aynı koddur.
/// </summary>
public static class InviteCodeHasher
{
    private const char EnDash = '–';

    /// <summary>Tüm boşluk karakterlerini (<see cref="char.IsWhiteSpace(char)"/>), <c>-</c> ve en dash'i siler, büyük harfe çevirir.</summary>
    public static string Normalize(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var builder = new StringBuilder(code.Length);
        foreach (var c in code)
        {
            if (char.IsWhiteSpace(c) || c == '-' || c == EnDash)
            {
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString().ToUpperInvariant();
    }

    /// <summary>SHA-256(UTF-8(<see cref="Normalize"/>(code))), 32 bayt.</summary>
    public static byte[] Hash(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(code)));
}
