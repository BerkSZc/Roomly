using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Roomly.Api.Common.Auth;
using Roomly.Api.Common.Errors;
using Roomly.Api.Common.Http;
using Roomly.Api.Features.Groups.Dtos;

namespace Roomly.Api.Features.Groups;

[ApiController]
[Route("api/groups")]
[Authorize]
public sealed class SpaceController(SpaceService service, IValidator<CreateSpaceRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSpaceRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblems.FromValidation(validation);
        }

        var result = await service.CreateAsync(User.GetUserId(), request.Name!, request.Type!.Value, request.Capacity, ct);
        return result.ToActionResult(space => CreatedAtAction(nameof(Get), new { groupId = space.Id }, space));
    }

    [HttpGet("{groupId:guid}")]
    public async Task<IActionResult> Get(Guid groupId, CancellationToken ct) =>
        (await service.GetAsync(groupId, User.GetUserId(), ct)).ToActionResult(Ok);

    [HttpPost("{groupId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid groupId, CancellationToken ct) =>
        (await service.LeaveAsync(groupId, User.GetUserId(), ct)).ToActionResult(Ok);
}
