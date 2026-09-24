using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NojectServer.Modules.Identity.Api.Contracts.PendingEmailVerification;
using NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Api;

/// <summary>
/// Exposes only the account-management actions permitted while an email is unverified.
/// </summary>
[ApiController]
[Route("auth/pending-email-verification")]
[Produces("application/json")]
[Authorize(Policy = IdentitySecurity.PendingEmailVerificationPolicy)]
public sealed class PendingEmailVerificationController(
    IPendingEmailVerificationService pendingEmailVerificationService)
    : ControllerBase
{
    private readonly IPendingEmailVerificationService _service =
        pendingEmailVerificationService;

    [HttpPost("resend")]
    public async Task<ActionResult> Resend(
        CancellationToken cancellationToken)
    {
        Result result = await _service.ResendAsync(
            User.GetUserId(),
            cancellationToken);

        return result.ToActionResult(this, StatusCodes.Status202Accepted);
    }

    [HttpPut("email")]
    public async Task<ActionResult> ChangeEmail(
        ChangePendingEmailRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _service.ChangeEmailAsync(
            User.GetUserId(),
            new ChangePendingEmailInput(request.NewEmail),
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete("account")]
    public async Task<ActionResult> DeleteAccount(
        DeletePendingAccountRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _service.DeleteAccountAsync(
            User.GetUserId(),
            new DeletePendingAccountInput(request.Password),
            cancellationToken);

        return result.ToActionResult(this);
    }
}
