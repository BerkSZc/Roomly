namespace Roomly.Api.Features.Auth;

/// <summary>
/// Auth tablolarındaki adlı index ve kısıtlar (spec 0002, Kısıt adları). Hata eşlemesinde
/// <c>PostgresException.ConstraintName</c> bu sabitlerle karşılaştırılır.
/// </summary>
public static class AuthDbNames
{
    public const string UsersTable = "asp_net_users";
    public const string UserClaimsTable = "asp_net_user_claims";
    public const string UserLoginsTable = "asp_net_user_logins";
    public const string UserTokensTable = "asp_net_user_tokens";

    public const string UsersNormalizedEmailUnique = "ux_asp_net_users_normalized_email";
    public const string UsersNormalizedUserNameUnique = "ux_asp_net_users_normalized_user_name";

    public const string RefreshTokensTokenHashUnique = "ux_refresh_tokens_token_hash";
    public const string RefreshTokensReplacedByIdUnique = "ux_refresh_tokens_replaced_by_id";
    public const string RefreshTokensTokenHashLength = "ck_refresh_tokens_token_hash_length";
    public const string RefreshTokensUserIdIndex = "ix_refresh_tokens_user_id";
    public const string RefreshTokensFamilyIdIndex = "ix_refresh_tokens_family_id";
}
