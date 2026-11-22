using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Roomly.Api.Features.Auth;

public sealed record GeneratedRefreshToken(string PlainText, byte[] Hash);

/// <summary>32 rastgele bayt (base64url, 43 karakter); DB'ye yalnızca SHA-256 özeti yazılır.</summary>
public static class RefreshTokenGenerator
{
    public const int TokenBytes = 32;

    public static GeneratedRefreshToken Generate()
    {
        var plain = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        return new GeneratedRefreshToken(plain, Hash(plain));
    }

    public static byte[] Hash(string plainText) => SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
}
