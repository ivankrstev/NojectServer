using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NojectServer.Configurations;
using NojectServer.Models;
using NojectServer.Models.Requests.Auth;
using NojectServer.Repositories.Interfaces;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Utils;
using NojectServer.Utils.ResultPattern;
using System.IdentityModel.Tokens.Jwt;

namespace NojectServer.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController(
    IUserRepository userRepository,
    IAuthService authService,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    ITwoFactorAuthService twoFactorAuthService,
    IOptions<JwtTokenOptions> options) : ControllerBase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IAuthService _authService = authService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ITwoFactorAuthService _twoFactorAuthService = twoFactorAuthService;
    private readonly JwtTokenOptions _jwtTokenOptions = options.Value;

    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost("register", Name = "Register user")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        return result.ToActionResult(this, _ => Created(nameof(User),
            new { message = "Registration successful. Please check your email to verify your account." }));
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("login", Name = "Login user")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var authResult = await _authService.LoginAsync(request);

        // Custom handling for login due to cookie setting and token generation
        if (authResult is not SuccessResult<User> success)
        {
            var failure = (FailureResult<User>)authResult;
            return StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message });
        }

        var user = success.Value;
        var accessToken = _tokenService.CreateAccessToken(user.Id, user.Email);
        var refreshTokenResult = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id, user.Email);

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
    public async Task<ActionResult> VerifyTfa(LoginTfaVerificationRequest request)
    {
        var userId = User.GetUserId();
        var email = User.GetUserEmail();

        var result = await _twoFactorAuthService.ValidateTwoFactorCodeAsync(userId, request.TwoFactorCode.Trim());

        return result.ToActionResult(this, isValid =>
            isValid ? Ok(new { access_token = _tokenService.CreateAccessToken(userId, email) })
                    : BadRequest(new { error = "Bad Request", message = "Invalid security code." }));
    }

    [HttpPost("tfa/generate")]
    [Authorize]
    public async Task<ActionResult> Generate2FaSetup()
    {
        var userId = User.GetUserId();
        var result = await _twoFactorAuthService.GenerateSetupCodeAsync(userId);

        return result.ToActionResult(this);
    }

    [HttpPut("tfa/enable")]
    [Authorize]
    public async Task<ActionResult> Enable2Fa(ToggleTfaRequest request)
    {
        var userId = User.GetUserId();
        var result = await _twoFactorAuthService.EnableTwoFactorAsync(userId, request.TwoFactorCode);

        return result.ToActionResult(this);
    }

    [HttpPut("tfa/disable")]
    [Authorize]
    public async Task<ActionResult> Disable2Fa(ToggleTfaRequest request)
    {
        var userId = User.GetUserId();
        var result = await _twoFactorAuthService.DisableTwoFactorAsync(userId, request.TwoFactorCode);

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
            var user = _userRepository.GetByIdAsync(validToken.UserId).Result;
            if (user == null)
            {
                return Unauthorized(new { error = "Unauthorized", message = "User not found." });
            }
            var accessToken = _tokenService.CreateAccessToken(validToken.UserId, user.Email);
            return Ok(new { access_token = accessToken });
        });
    }

    [HttpGet("verify-email")]
    public async Task<ActionResult<object>> VerifyEmail(VerifyEmailRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request.Email, request.Token);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(EmailOnlyRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);

        return result.ToActionResult(this, value => Ok(new { message = value }));
    }
}
