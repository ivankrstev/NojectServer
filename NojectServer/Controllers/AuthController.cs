using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NojectServer.Configurations;
using NojectServer.Data;
using NojectServer.Models;
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
    DataContext dataContext,
    IAuthService authService,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    ITwoFactorAuthService twoFactorAuthService,
    IOptions<JwtTokenOptions> options) : ControllerBase
{
    private readonly DataContext _dataContext = dataContext;
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
        return result switch
        {
            SuccessResult<User> => Created(nameof(User),
                new { message = "Registration successful. Please check your email to verify your account." }),
            FailureResult<User> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("login", Name = "Login user")]
    public async Task<IActionResult> Login(UserLoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result is not SuccessResult<string> success)
        {
            var failure = (FailureResult<string>)result;
            return StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message });
        }

        var email = success.Value;
        var accessToken = _tokenService.CreateAccessToken(email);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(email);

        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddSeconds(_jwtTokenOptions.Access.ExpirationInSeconds),
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

        return result switch
        {
            SuccessResult<bool> success => success.Value
                ? Ok(new { access_token = _tokenService.CreateAccessToken(email) })
                : BadRequest(new { error = "Bad Request", message = "Invalid security code." }),
            FailureResult<bool> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpPost("tfa/generate")]
    [Authorize]
    public async Task<ActionResult> Generate2FaSetup()
    {
        var email = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _twoFactorAuthService.GenerateSetupCodeAsync(email);

        return result switch
        {
            SuccessResult<TwoFactorSetupResult> success => Ok(success.Value),
            FailureResult<TwoFactorSetupResult> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpPut("tfa/enable")]
    [Authorize]
    public async Task<ActionResult> Enable2Fa(UserToggleTfaRequest request)
    {
        var email = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _twoFactorAuthService.EnableTwoFactorAsync(email, request.TwoFactorCode);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpPut("tfa/disable")]
    [Authorize]
    public async Task<ActionResult> Disable2Fa(UserToggleTfaRequest request)
    {
        var email = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _twoFactorAuthService.DisableTwoFactorAsync(email, request.TwoFactorCode);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("refresh-token", Name = "Refresh Token")]
    public async Task<ActionResult<object>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        var user = await _dataContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (user == null)
            return Unauthorized(new
            {
                error = "Unauthorized",
                message = "Invalid refresh token."
            });

        if (user.ExpireDate < DateTime.UtcNow)
            return Unauthorized(new
            {
                error = "Unauthorized",
                message = "Token has expired."
            });

        var accessToken = _tokenService.CreateAccessToken(user.Email);
        return new { accessToken };
    }

    [HttpGet("verify-email")]
    public async Task<ActionResult<object>> VerifyEmail([FromQuery] string email, [FromQuery] string token)
    {
        var result = await _authService.VerifyEmailAsync(email, token);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword(EmailOnlyRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Email);

        return result switch
        {
            SuccessResult<string> success => Ok(new { message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }
}