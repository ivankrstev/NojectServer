using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Data;
using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.Services.Email;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NojectServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _dataContext;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(DataContext dataContext, IConfiguration configuration, IEmailService emailService)
        {
            _dataContext = dataContext;
            _config = configuration;
            _emailService = emailService;
        }

        [HttpPost("register", Name = "Register user")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(UserRegisterRequest request)
        {
            if (await _dataContext.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict(new
                {
                    error = "User already exists",
                    message = "A user with the provided email already exists"
                });
            }
            var requestError = request.Validate();
            if (requestError != null)
            {
                return BadRequest(requestError);
            }
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            User user = new()
            {
                Email = request.Email,
                Password = passwordHash,
                PasswordSalt = passwordSalt,
                FullName = request.FullName,
                VerificationToken = GenerateRandomToken()
            };
            _dataContext.Add(user);
            await _dataContext.SaveChangesAsync();
            _emailService.SendVerificationLink(user);
            return Created(nameof(User), new { message = "Registration successful. Please check your email to verify your account" });
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("login", Name = "Login user")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized(new
                {
                    error = "Invalid credentials",
                    message = "The provided username and password combination is incorrect."
                });
            }
            if (user.VerifiedAt == null)
            {
                return Unauthorized(new
                {
                    error = "Email not verified",
                    message = "Please verify your email before proceeding."
                });
            }
            if (!VerifyPasswordHash(request.Password, user.Password, user.PasswordSalt))
            {
                return Unauthorized(new
                {
                    error = "Invalid credentials",
                    message = "The provided username and password combination is incorrect."
                });
            }
            string token = CreateRefreshToken(user);
            RefreshToken refreshToken = new()
            {
                Email = user.Email,
                Token = token
            };
            _dataContext.Add(refreshToken);
            await _dataContext.SaveChangesAsync();
            Response.Cookies.Append("refresh_token", token, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(14),
                Secure = true,
                SameSite = SameSiteMode.None
            });
            return Ok(new
            {
                access_token = CreateAccessToken(user)
            });
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost("refresh-token", Name = "Refresh Token")]
        public async Task<ActionResult<object>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            var user = await _dataContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (user == null)
            {
                return Unauthorized(new
                {
                    error = "Unauthorized",
                    message = "Invalid refresh token"
                });
            }
            else if (user.ExpireDate < DateTime.UtcNow)
            {
                return Unauthorized(new
                {
                    error = "Unauthorized",
                    message = "Token expired"
                });
            }
            string accessToken = CreateAccessToken(new User { Email = user.Email });
            return new { accessToken };
        }

        [HttpGet("verify-email")]
        public async Task<ActionResult<object>> VerifyEmail([FromQuery] string email, [FromQuery] string token)
        {
            var foundUser = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.VerificationToken == token);
            if (foundUser == null)
            {
                return BadRequest(new
                {
                    error = "Invalid Verification Information",
                    message = "The verification token or email you provided is not valid"
                });
            }
            else if (foundUser.VerifiedAt != null)
            {
                return BadRequest(new
                {
                    error = "Email Already Verified",
                    message = "The email associated with this account has already been verified"
                });
            }
            foundUser.VerifiedAt = DateTime.UtcNow;
            _dataContext.Update(foundUser);
            await _dataContext.SaveChangesAsync();
            return Ok(new { message = "Email Successfully Verified" });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(EmailOnlyRequest request)
        {
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User not found",
                    message = "User with the specified email was not found"
                });
            }
            user.PasswordResetToken = GenerateRandomToken();
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _dataContext.SaveChangesAsync();
            _emailService.SendResetPasswordLink(user);
            return Ok(new { message = "Reset link was sent to your email" });
        }

        private string CreateTfaToken(string email)
        {
            List<Claim> claims = new() {
                new Claim(ClaimTypes.Name, email)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:TfaToken"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: creds,
                expires: DateTime.UtcNow.AddMinutes(2)
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        private string CreateRefreshToken(User user)
        {
            List<Claim> claims = new() {
                new Claim(ClaimTypes.Name, user.Email)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:RefreshToken"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: creds,
                expires: DateTime.Now.AddDays(14)
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        private string CreateAccessToken(User user)
        {
            List<Claim> claims = new() {
                new Claim(ClaimTypes.Name, user.Email)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTSecrets:AccessToken"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                signingCredentials: creds,
                expires: DateTime.Now.AddMinutes(10)
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using HMACSHA512 hmac = new();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using HMACSHA512 hmac = new(passwordSalt);
            byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }

        private static string GenerateRandomToken(int length = 128)
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(length);
            string token = Convert.ToBase64String(randomBytes);
            token = token.Replace("+", "").Replace("/", "").Replace("=", "");
            return token[..length];
        }
    }
}