using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Roomly.Api.Common.Time;

namespace Roomly.Api.Features.Auth;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface IAccessTokenIssuer
{
    AccessToken Issue(Guid userId);
}

/// <summary>HS256 JWT: <c>sub</c>, <c>jti</c>, <c>iss</c>, <c>aud</c>, <c>exp = IClock.UtcNow + 15 dk</c>.</summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, IClock clock) : IAccessTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken Issue(Guid userId)
    {
        var now = clock.UtcNow;
        var expires = now + _options.AccessTokenLifetime;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_options.SigningKeyBytes), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
