using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NojectServer.Modules.Identity.Api.Contracts.EmailVerification;
using NojectServer.Modules.Identity.Application.Authentication.EmailVerification;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Api;

/// <summary>
/// Requests and consumes opaque email-verification tokens.
/// </summary>
[ApiController]
[Route("auth/email-verification")]
[Produces("application/json")]
public sealed class EmailVerificationController(
    IEmailVerificationService emailVerificationService)
    : ControllerBase
{
    private readonly IEmailVerificationService _emailVerificationService =
        emailVerificationService;

    [AllowAnonymous]
    [HttpPost("request")]
    public async Task<ActionResult> RequestVerification(
        RequestEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _emailVerificationService.RequestVerificationAsync(
            new RequestEmailVerificationInput(request.Email),
            cancellationToken);

        return result.ToActionResult(this, StatusCodes.Status202Accepted);
    }

    [AllowAnonymous]
    [HttpPost("verify")]
    public async Task<ActionResult> Verify(
        VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await _emailVerificationService.VerifyAsync(
            new VerifyEmailInput(request.Email, request.Token),
            cancellationToken);

        return result.ToActionResult(this);
    }
}
