namespace Roomly.Api.Features.Groups.Dtos;

public sealed record CreateSpaceRequest(string? Name, GroupType? Type, int Capacity);

public sealed record SpaceDetails(
    Guid Id, string Name, GroupType Type, MembershipRole Role, int Capacity, int MemberCount);

public sealed record LeaveSpaceResponse(bool Archived, bool AdminTransferred);
