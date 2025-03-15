using System.Security.Cryptography;

namespace NojectServer.Utils;

public class TokenGenerator
{
    public static string GenerateRandomToken(int length = 128)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(length);
        var token = Convert.ToBase64String(randomBytes);
        token = token.Replace("+", "").Replace("/", "").Replace("=", "");
        return token[..length];
    }
}