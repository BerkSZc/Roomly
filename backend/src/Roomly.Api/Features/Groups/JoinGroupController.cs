using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Roomly.Api.Common.Auth;
using Roomly.Api.Common.Errors;
using Roomly.Api.Common.Http;
using Roomly.Api.Features.Groups.Dtos;

namespace Roomly.Api.Features.Groups;

[ApiController]
[Route("api/groups")]
[Authorize]
public sealed class JoinGroupController(
    JoinGroupService service,
    IValidator<JoinGroupRequest> validator) : ControllerBase
{
    [HttpPost("join")]
    [EnableRateLimiting(GroupsServiceCollectionExtensions.JoinRateLimitPolicy)]
    public async Task<IActionResult> Join(JoinGroupRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblems.FromValidation(validation);
        }

        return (await service.JoinAsync(User.GetUserId(), request.Code!, ct)).ToActionResult(Ok);
    }
}
