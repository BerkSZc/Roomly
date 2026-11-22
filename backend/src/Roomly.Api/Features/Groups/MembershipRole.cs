namespace Roomly.Api.Features.Groups;

/// <summary>Veritabanında metin olarak saklanır (en fazla 10 karakter). Grupta tek <see cref="Admin"/>.</summary>
public enum MembershipRole
{
    Admin,
    Member,
}
