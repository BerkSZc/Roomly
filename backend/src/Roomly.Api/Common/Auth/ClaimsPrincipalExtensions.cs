using System.Security.Claims;

namespace Roomly.Api.Common.Auth;

/// <summary>Korumalı bir uçta oturumdaki kullanıcının kimliği: <c>User.GetUserId()</c>.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>JWT <c>sub</c> claim'i (spec 0004). Yalnızca <c>[Authorize]</c> altındaki uçlarda çağrılır.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new InvalidOperationException("Oturumda geçerli bir 'sub' claim'i yok.");
    }
}
