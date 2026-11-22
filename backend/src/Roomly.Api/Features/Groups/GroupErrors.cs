using Roomly.Api.Common.Results;

namespace Roomly.Api.Features.Groups;

/// <summary>Katılım hata kodları (spec 0006). Öncelik: <see cref="JoinGroupService"/>.</summary>
public static class GroupErrors
{
    public const string InviteInvalidCode = "INVITE_INVALID";
    public const string InviteExpiredCode = "INVITE_EXPIRED";
    public const string InviteRevokedCode = "INVITE_REVOKED";
    public const string AlreadyMemberCode = "ALREADY_MEMBER";
    public const string AlreadyInGroupCode = "ALREADY_IN_GROUP";
    public const string CapacityExceededCode = "CAPACITY_EXCEEDED";

    public static readonly Error InviteInvalid = new(
        InviteInvalidCode, "Bu davet kodu geçersiz.", StatusCodes.Status404NotFound);

    public static readonly Error InviteExpired = new(
        InviteExpiredCode, "Bu davet kodunun süresi dolmuş.", StatusCodes.Status410Gone);

    public static readonly Error InviteRevoked = new(
        InviteRevokedCode, "Bu davet kodu iptal edilmiş.", StatusCodes.Status410Gone);

    public static readonly Error AlreadyMember = Error.Conflict(
        AlreadyMemberCode, "Zaten bu grubun üyesisiniz.");

    public static readonly Error AlreadyInGroup = Error.Conflict(
        AlreadyInGroupCode, "Zaten başka bir grubun üyesisiniz.");

    public static readonly Error CapacityExceeded = Error.Conflict(
        CapacityExceededCode, "Bu grup dolu.");
}
