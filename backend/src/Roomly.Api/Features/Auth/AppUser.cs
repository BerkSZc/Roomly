using Microsoft.AspNetCore.Identity;

namespace Roomly.Api.Features.Auth;

/// <summary>
/// Identity kullanıcısı. Kişisel veri yalnızca <see cref="DisplayName"/> ve e-posta
/// (<c>UserName</c> e-postanın kopyasıdır). Satır silinmez, hesap silmede anonimleştirilir.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary><c>DeletedUser_&lt;Guid&gt;</c> (48 karakter) sığar.</summary>
    public const int DisplayNameMaxLength = 50;

    public required string DisplayName { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
