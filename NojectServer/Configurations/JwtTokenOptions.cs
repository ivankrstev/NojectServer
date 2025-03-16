namespace NojectServer.Configurations;

public class JwtTokenOptions
{
    public required string Issuer { get; set; }

    public required string Audience { get; set; }

    public required JwtSigningCredentials Access { get; set; }

    public required JwtSigningCredentials Refresh { get; set; }

    public required JwtSigningCredentials Tfa { get; set; }

    public class JwtSigningCredentials
    {
        public required string SecretKey { get; set; }
        public required int ExpirationInSeconds { get; set; }
    }
}
