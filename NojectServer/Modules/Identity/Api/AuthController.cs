using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NojectServer.Modules.Identity.Api.Contracts.Authentication;
using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Application.Authentication.Register;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Api;

/// <summary>
/// Exposes account registration and the password and two-factor login workflow.
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController(
    ILoginService loginService,
    IRegistrationService registrationService) : ControllerBase
{
    private readonly ILoginService _loginService = loginService;
    private readonly IRegistrationService _registrationService = registrationService;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        Result<RegisteredUser> result = await _registrationService.RegisterAsync(
            new RegisterInput(
                request.Email,
                request.FullName,
                request.Password,
                request.ConfirmPassword),
            cancellationToken);

        return result.ToActionResult(this, StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        Result<LoginResult> result = await _loginService.LoginAsync(
            new LoginInput(request.Email, request.Password),
            cancellationToken);

        return result.ToActionResult(this, MapLoginResult);
    }

    [AllowAnonymous]
    [HttpPost("login/two-factor")]
    public async Task<ActionResult> CompleteTwoFactorLogin(
        CompleteTwoFactorLoginRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthenticatedLoginResult> result =
            await _loginService.CompleteTwoFactorLoginAsync(
                new CompleteTwoFactorLoginInput(
                    request.TfaToken,
                    request.Code),
                cancellationToken);

        return result.ToActionResult(
            this,
            authenticated => MapAuthenticatedLogin(authenticated));
    }

    private ActionResult MapLoginResult(LoginResult loginResult)
    {
        return loginResult switch
        {
            AuthenticatedLoginResult authenticated =>
                MapAuthenticatedLogin(authenticated),

            TwoFactorRequiredLoginResult twoFactorRequired =>
                Ok(new TwoFactorRequiredResponse(
                    Status: "two_factor_required",
                    TwoFactorToken: twoFactorRequired.TwoFactorToken,
                    ExpiresAt: twoFactorRequired.ExpiresAt)),

            EmailVerificationRequiredLoginResult verificationRequired =>
                Ok(new EmailVerificationRequiredResponse(
                    Status: "email_verification_required",
                    PendingVerificationToken:
                        verificationRequired.PendingVerificationToken,
                    ExpiresAt: verificationRequired.ExpiresAt,
                    MaskedEmail: verificationRequired.MaskedEmail)),

            _ => throw new InvalidOperationException(
                $"Unsupported login result type: {loginResult.GetType().Name}.")
        };
    }

    private ActionResult MapAuthenticatedLogin(
        AuthenticatedLoginResult authenticated)
    {
        AppendRefreshTokenCookie(authenticated);

        return Ok(new AuthenticatedLoginResponse(
            Status: "authenticated",
            AccessToken: authenticated.AccessToken,
            AccessTokenExpiresAt: authenticated.AccessTokenExpiresAt,
            RefreshTokenExpiresAt: authenticated.RefreshTokenExpiresAt));
    }

    private void AppendRefreshTokenCookie(
        AuthenticatedLoginResult authenticated)
    {
        Response.Cookies.Append(
            "__Secure-refresh_token",
            authenticated.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/auth",
                Expires = authenticated.RefreshTokenExpiresAt
            });
    }
}
