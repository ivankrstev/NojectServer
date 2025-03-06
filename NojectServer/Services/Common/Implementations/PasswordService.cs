using NojectServer.Services.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace NojectServer.Services.Common.Implementations;

public class PasswordService : IPasswordService
{
    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
    }

    public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        // Check for null hash or salt
        if (passwordHash == null || passwordSalt == null)
            return false;
        using var hmac = new HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
        return computedHash.SequenceEqual(passwordHash);
    }
}