using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NojectServer.Modules.Identity.Api.Contracts.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Api;

[ApiController]
[Route("auth/two-factor")]
[Produces("application/json")]
[Authorize]
public sealed class TwoFactorAuthController(
    ITwoFactorAuthService twoFactorAuthService) : ControllerBase
{
    private readonly ITwoFactorAuthService _twoFactorAuthService =
        twoFactorAuthService;

    [HttpPost("setup")]
    public async Task<ActionResult> Setup(CancellationToken cancellationToken)
    {
        Result<TwoFactorSetup> result =
            await _twoFactorAuthService.GenerateSetupCodeAsync(
                User.GetUserId(),
                cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("enable")]
    public async Task<ActionResult> Enable(
        ToggleTfaRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _twoFactorAuthService.EnableAsync(
            User.GetUserId(),
            request.Code,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("disable")]
    public async Task<ActionResult> Disable(
        ToggleTfaRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _twoFactorAuthService.DisableAsync(
            User.GetUserId(),
            request.Code,
            cancellationToken);

        return result.ToActionResult(this);
    }
}
