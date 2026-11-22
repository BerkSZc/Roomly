using Roomly.Api.Features.Auth.Dtos;

namespace Roomly.Api.Features.Groups.Dtos;

public sealed record JoinGroupRequest(string? Code);

public sealed record JoinGroupResponse(GroupSummaryDto Group);
