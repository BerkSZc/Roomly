using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Roomly.Api.Common.Auth;
using Roomly.Api.Common.Errors;
using Roomly.Api.Common.Http;
using Roomly.Api.Features.Auth.Dtos;

namespace Roomly.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthRateLimiting.PolicyName)]
public sealed class AuthController(
    AuthService service,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var validation = await registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblems.FromValidation(validation);
        }

        var result = await service.RegisterAsync(request, ct);
        return result.ToActionResult(r => StatusCode(StatusCodes.Status201Created, r));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var validation = await loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblems.FromValidation(validation);
        }

        return (await service.LoginAsync(request, ct)).ToActionResult(Ok);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var validation = await refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblems.FromValidation(validation);
        }

        return (await service.RefreshAsync(request, ct)).ToActionResult(Ok);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        var validation = await refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblems.FromValidation(validation);
        }

        await service.LogoutAsync(request, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        (await service.MeAsync(User.GetUserId(), ct)).ToActionResult(Ok);
}
