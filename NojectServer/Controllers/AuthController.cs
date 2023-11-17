using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NojectServer.Data;
using NojectServer.Models;
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

        public AuthController(DataContext dataContext, IConfiguration configuration)
        {
            _dataContext = dataContext;
            _config = configuration;
        }

        [HttpPost("register", Name = "Register user")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(UserRegisterRequest request)
        {
            if (_dataContext.Users.Any(u => u.Email == request.Email))
            {
                return Conflict("User already exists");
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
            return Created(nameof(User), new { email = user.Email });
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
                return BadRequest("User doesn't exist");
            }
            if (user.VerifiedAt == null)
            {
                return BadRequest("Your email address has not been verified");
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
                Expires = DateTime.Now.AddDays(14)
            });
            return Ok(new
            {
                access_token = CreateAccessToken(user)
            });
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

        private static string GenerateRandomToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(100);
            string token = Convert.ToBase64String(randomBytes);
            token = token.Replace("+", "").Replace("/", "").Replace("=", "");
            return token[..100];
        }
    }
}