using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NojectServer.Configurations;
using NojectServer.Models.Requests;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Utils.ResultPattern;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NojectServer.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    ITwoFactorAuthService twoFactorAuthService,
    IOptions<JwtTokenOptions> options) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ITwoFactorAuthService _twoFactorAuthService = twoFactorAuthService;
    private readonly JwtTokenOptions _jwtTokenOptions = options.Value;

    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost("register", Name = "Register user")]
    public async Task<IActionResult> Register(UserRegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        return result.ToActionResult(this, _ => Created(nameof(User),
            new { message = "Registration successful. Please check your email to verify your account." }));
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("login", Name = "Login user")]
    public async Task<IActionResult> Login(UserLoginRequest request)
    {
        var authResult = await _authService.LoginAsync(request);

        // Custom handling for login due to cookie setting and token generation
        if (authResult is not SuccessResult<string> success)
        {
            var failure = (FailureResult<string>)authResult;
            return StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message });
        }

        var email = success.Value;
        var accessToken = _tokenService.CreateAccessToken(email);
        var refreshTokenResult = await _refreshTokenService.GenerateRefreshTokenAsync(email);

        if (refreshTokenResult is not SuccessResult<string> refreshSuccess)
        {
            var failure = (FailureResult<string>)refreshTokenResult;
            return StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message });
        }

        var refreshToken = refreshSuccess.Value;

        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddSeconds(_jwtTokenOptions.Refresh.ExpirationInSeconds),
            Secure = true,
            SameSite = SameSiteMode.None
        });

        return Ok(new { access_token = accessToken });
    }

    [HttpPost("tfa/verify", Name = "Verify the two-factor code to login")]
    public async Task<ActionResult> VerifyTfa(UserVerifyTfaRequest request)
    {
        var principal = new JwtSecurityTokenHandler().ValidateToken(request.JwtToken,
            _tokenService.GetTfaTokenValidationParameters(), out _);
        var email = principal.FindFirst(ClaimTypes.Name)?.Value!;

        var result = await _twoFactorAuthService.ValidateTwoFactorCodeAsync(email, request.TwoFactorCode.Trim());

        return result.ToActionResult(this, isValid =>
            isValid ? Ok(new { access_token = _tokenService.CreateAccessToken(email) })
                    : BadRequest(new { error = "Bad Request", message = "Invalid security code." }));
    }

    [HttpPost("tfa/generate")]
    [Authorize]
    public async Task<ActionResult> Generate2FaSetup()
    {
        var email = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _twoFactorAuthService.GenerateSetupCodeAsync(email);

        return result.ToActionResult(this);
    }

    [HttpPut("tfa/enable")]
    [Authorize]
    public async Task<ActionResult> Enable2Fa(UserToggleTfaRequest request)
    {
        var email = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _twoFactorAuthService.EnableTwoFactorAsync(email, request.TwoFactorCode);

        return result.ToActionResult(this);
    }

    [HttpPut("tfa/disable")]
    [Authorize]
    public async Task<ActionResult> Disable2Fa(UserToggleTfaRequest request)
    {
        var email = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _twoFactorAuthService.DisableTwoFactorAsync(email, request.TwoFactorCode);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("refresh-token", Name = "Refresh Token")]
    public async Task<ActionResult<object>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new
            {
                error = "Unauthorized",
                message = "Refresh token not found."
            });

        var result = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken);

        return result.ToActionResult(this, validToken =>
        {
            var accessToken = _tokenService.CreateAccessToken(validToken.Email);
            return Ok(new { access_token = accessToken });
        });
    }

    [HttpGet("verify-email")]
    public async Task<ActionResult<object>> VerifyEmail([FromQuery] string email, [FromQuery] string token)
    {
        var result = await _authService.VerifyEmailAsync(email, token);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(EmailOnlyRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }
}
