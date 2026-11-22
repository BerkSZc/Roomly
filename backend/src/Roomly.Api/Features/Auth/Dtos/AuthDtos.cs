using Roomly.Api.Features.Groups;

namespace Roomly.Api.Features.Auth.Dtos;

public sealed record RegisterRequest(string? DisplayName, string? Email, string? Password);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record UserDto(Guid Id, string DisplayName, string Email);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);

public sealed record GroupSummaryDto(Guid Id, string Name, GroupType Type, MembershipRole Role);

public sealed record MeResponse(Guid Id, string DisplayName, string Email, GroupSummaryDto? Group);
