using Microsoft.AspNetCore.Mvc;
using NojectServer.Data;
using NojectServer.Models;
using System.Security.Cryptography;

namespace NojectServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly DataContext _dataContext;

        public UsersController(DataContext dataContext) => _dataContext = dataContext;

        [HttpPost("register", Name = "Register user")]
        public async Task<IActionResult> Register(UserRegisterRequest request)
        {
            if (_dataContext.Users.Any(u => u.Email == request.Email))
            {
                return Conflict("User already exists");
            }
            List<string> requestErrors = request.Validate();
            if (requestErrors.Any())
            {
                return BadRequest(new { errors = requestErrors });
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

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using HMACSHA512 hmac = new();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
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