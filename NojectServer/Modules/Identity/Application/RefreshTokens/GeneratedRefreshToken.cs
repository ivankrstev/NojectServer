namespace NojectServer.Modules.Identity.Application.RefreshTokens;

public record GeneratedRefreshToken(string PlainTextToken, byte[] Hash);
