using Microsoft.Extensions.Logging.Abstractions;
using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Application.JwtTokens;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.UnitTests.Identity.Application.Authentication.Login;

internal sealed class LoginServiceTestContext
{
    public LoginServiceTestContext()
    {
        Service = new LoginService(
            new LoginInputValidator(),
            new CompleteTwoFactorLoginInputValidator(),
            UserRepository,
            PasswordHasher,
            JwtTokenService,
            RefreshTokenService,
            TwoFactorAuthService,
            NullLogger<LoginService>.Instance);
    }

    public RecordingUserRepository UserRepository { get; } = new();

    public RecordingPasswordHasher PasswordHasher { get; } = new();

    public RecordingJwtTokenService JwtTokenService { get; } = new();

    public RecordingRefreshTokenService RefreshTokenService { get; } = new();

    public RecordingTwoFactorAuthService TwoFactorAuthService { get; } = new();

    public LoginService Service { get; }
}

internal sealed class RecordingUserRepository : IUserRepository
{
    public User? UserByEmail { get; set; }

    public User? UserById { get; set; }

    public string? RequestedEmail { get; private set; }

    public Guid RequestedUserId { get; private set; }

    public CancellationToken GetByEmailCancellationToken { get; private set; }

    public CancellationToken GetByIdCancellationToken { get; private set; }

    public int GetByEmailCallCount { get; private set; }

    public int GetByIdCallCount { get; private set; }

    public Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        GetByIdCallCount++;
        RequestedUserId = userId;
        GetByIdCancellationToken = cancellationToken;
        return Task.FromResult(UserById);
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        GetByEmailCallCount++;
        RequestedEmail = email;
        GetByEmailCancellationToken = cancellationToken;
        return Task.FromResult(UserByEmail);
    }

    public Task<User?> GetByPasswordResetTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public void Add(User user)
    {
        throw new NotSupportedException();
    }

    public Task<bool> DeleteUnverifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}

internal sealed class RecordingPasswordHasher : IPasswordHasher
{
    public byte[] DummyHash { get; } = [11, 12, 13];

    public byte[] DummySalt { get; } = [21, 22, 23];

    public bool VerifyResult { get; set; }

    public string? HashedPassword { get; private set; }

    public string? VerifiedPassword { get; private set; }

    public byte[]? VerifiedHash { get; private set; }

    public byte[]? VerifiedSalt { get; private set; }

    public int HashCallCount { get; private set; }

    public int VerifyCallCount { get; private set; }

    public HashedPassword Hash(string password)
    {
        HashCallCount++;
        HashedPassword = password;
        return new HashedPassword(DummyHash, DummySalt);
    }

    public bool Verify(string password, byte[] hash, byte[] salt)
    {
        VerifyCallCount++;
        VerifiedPassword = password;
        VerifiedHash = hash;
        VerifiedSalt = salt;
        return VerifyResult;
    }
}

internal sealed class RecordingJwtTokenService : IJwtTokenService
{
    public GeneratedJwtToken AccessToken { get; set; } = new(
        "access-token",
        new DateTimeOffset(2026, 9, 24, 13, 0, 0, TimeSpan.Zero));

    public GeneratedJwtToken TfaToken { get; set; } = new(
        "tfa-token",
        new DateTimeOffset(2026, 9, 24, 13, 5, 0, TimeSpan.Zero));

    public GeneratedJwtToken PendingVerificationToken { get; set; } = new(
        "pending-token",
        new DateTimeOffset(2026, 9, 24, 13, 10, 0, TimeSpan.Zero));

    public Result<TfaTokenClaims> TfaValidationResult { get; set; } =
        Result.Failure<TfaTokenClaims>(
            "Token.Invalid",
            "The token is invalid.",
            401);

    public Guid AccessTokenUserId { get; private set; }

    public Guid TfaTokenUserId { get; private set; }

    public Guid PendingVerificationTokenUserId { get; private set; }

    public string? ValidatedTfaToken { get; private set; }

    public int CreateAccessTokenCallCount { get; private set; }

    public int CreateTfaTokenCallCount { get; private set; }

    public int CreatePendingVerificationTokenCallCount { get; private set; }

    public int ValidateTfaTokenCallCount { get; private set; }

    public GeneratedJwtToken CreateAccessToken(Guid userId)
    {
        CreateAccessTokenCallCount++;
        AccessTokenUserId = userId;
        return AccessToken;
    }

    public GeneratedJwtToken CreateTfaToken(Guid userId)
    {
        CreateTfaTokenCallCount++;
        TfaTokenUserId = userId;
        return TfaToken;
    }

    public GeneratedJwtToken CreatePendingEmailVerificationToken(Guid userId)
    {
        CreatePendingVerificationTokenCallCount++;
        PendingVerificationTokenUserId = userId;
        return PendingVerificationToken;
    }

    public Result<TfaTokenClaims> ValidateTfaToken(string token)
    {
        ValidateTfaTokenCallCount++;
        ValidatedTfaToken = token;
        return TfaValidationResult;
    }
}

internal sealed class RecordingRefreshTokenService : IRefreshTokenService
{
    public IssuedRefreshToken IssuedToken { get; } = new(
        "refresh-token",
        new DateTimeOffset(2026, 9, 24, 14, 0, 0, TimeSpan.Zero));

    public Result<IssuedRefreshToken> IssueResult { get; set; } =
        Result.Success(new IssuedRefreshToken(
            "refresh-token",
            new DateTimeOffset(2026, 9, 24, 14, 0, 0, TimeSpan.Zero)));

    public Guid IssuedForUserId { get; private set; }

    public CancellationToken IssueCancellationToken { get; private set; }

    public int IssueCallCount { get; private set; }

    public Task<Result<IssuedRefreshToken>> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IssueCallCount++;
        IssuedForUserId = userId;
        IssueCancellationToken = cancellationToken;
        return Task.FromResult(IssueResult);
    }

    public Task<Result<RotatedRefreshToken>> RotateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<Result<bool>> RevokeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<Result> RevokeAllForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}

internal sealed class RecordingTwoFactorAuthService : ITwoFactorAuthService
{
    public Result CodeValidationResult { get; set; } = Result.Success();

    public Guid ValidatedUserId { get; private set; }

    public string? ValidatedCode { get; private set; }

    public CancellationToken ValidationCancellationToken { get; private set; }

    public int ValidateCodeCallCount { get; private set; }

    public Task<Result<TwoFactorSetup>> GenerateSetupCodeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<Result> EnableAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<Result> DisableAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<Result> ValidateCodeAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        ValidateCodeCallCount++;
        ValidatedUserId = userId;
        ValidatedCode = code;
        ValidationCancellationToken = cancellationToken;
        return Task.FromResult(CodeValidationResult);
    }
}
