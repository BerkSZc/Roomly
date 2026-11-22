namespace Roomly.Api.Features.Groups;

/// <summary>
/// Groups tablolarındaki adlı index ve kısıtlar (spec 0002, Kısıt adları). Hata eşlemesinde
/// <c>PostgresException.ConstraintName</c> bu sabitlerle karşılaştırılır.
/// </summary>
public static class GroupsDbNames
{
    public const string GroupsCapacityByType = "ck_groups_capacity_by_type";

    public const string MembershipsPrimaryKey = "pk_group_memberships";
    public const string MembershipsUserIdUnique = "ux_group_memberships_user_id";
    public const string MembershipsOneAdmin = "ux_group_memberships_one_admin";
    public const string MembershipsRole = "ck_group_memberships_role";
    public const string MembershipsGroupIdJoinedAtIndex = "ix_group_memberships_group_id_joined_at";

    public const string InviteCodesOneActive = "ux_invite_codes_one_active";
    public const string InviteCodesCodeHashUnique = "ux_invite_codes_code_hash";
    public const string InviteCodesCodeHashLength = "ck_invite_codes_code_hash_length";
}
