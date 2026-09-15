using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NojectServer.Modules.Identity.Api.Contracts.Passwords;
using NojectServer.Modules.Identity.Application.Authentication.PasswordReset;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Api;

[ApiController]
[Route("auth/password-reset")]
[Produces("application/json")]
public sealed class PasswordResetController(
    IPasswordResetService passwordResetService) : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService =
        passwordResetService;

    [AllowAnonymous]
    [HttpPost("request")]
    public async Task<ActionResult> RequestReset(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _passwordResetService.RequestResetAsync(
            new RequestPasswordResetInput(request.Email),
            cancellationToken);

        return result.ToActionResult(this, StatusCodes.Status202Accepted);
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    public async Task<ActionResult> Complete(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _passwordResetService.ResetPasswordAsync(
            new ResetPasswordInput(
                request.ResetToken,
                request.NewPassword,
                request.ConfirmNewPassword),
            cancellationToken);

        return result.ToActionResult(this);
    }
}
