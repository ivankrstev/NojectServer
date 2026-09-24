using Microsoft.Extensions.Logging.Abstractions;
using NojectServer.Modules.Identity.Application.Authentication.PasswordReset;
using NojectServer.Modules.Identity.Application.Email;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.Persistence;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Application.Tokens;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.UnitTests.Identity.Application.Authentication.PasswordReset;

public sealed class PasswordResetServiceTests
{
    [Fact]
    public async Task RequestResetAsync_WithNullInput_ThrowsArgumentNullException()
    {
        PasswordResetServiceTestContext context = CreateContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.Service.RequestResetAsync(
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RequestResetAsync_WithInvalidInput_ReturnsValidationFailure()
    {
        PasswordResetServiceTestContext context = CreateContext();

        Result result = await context.Service.RequestResetAsync(
            new RequestPasswordResetInput(null),
            TestContext.Current.CancellationToken);

        ValidationFailureResult failure =
            Assert.IsType<ValidationFailureResult>(result);
        Assert.Contains(
            nameof(RequestPasswordResetInput.Email),
            failure.ValidationErrors.Keys);
        Assert.Equal(0, context.UserRepository.GetByEmailCallCount);
        Assert.Equal(0, context.TokenGenerator.GenerateCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
        Assert.Equal(0, context.EmailService.SendResetPasswordLinkCallCount);
    }

    [Fact]
    public async Task RequestResetAsync_WhenEmailIsNotRegistered_ReturnsSuccessWithoutSendingEmail()
    {
        PasswordResetServiceTestContext context = CreateContext();
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result result = await context.Service.RequestResetAsync(
            new RequestPasswordResetInput("missing@example.com"),
            cancellationToken);

        Assert.IsType<SuccessResult>(result);
        Assert.Equal("missing@example.com", context.UserRepository.RequestedEmail);
        Assert.Equal(
            cancellationToken,
            context.UserRepository.GetByEmailCancellationToken);
        Assert.Equal(0, context.TokenGenerator.GenerateCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
        Assert.Equal(0, context.EmailService.SendResetPasswordLinkCallCount);
    }

    [Fact]
    public async Task RequestResetAsync_WhenEmailIsRegistered_PersistsTokenAndSendsEmail()
    {
        PasswordResetServiceTestContext context = CreateContext();
        User user = CreateUser("person@example.com");
        context.UserRepository.UserByEmail = user;
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result result = await context.Service.RequestResetAsync(
            new RequestPasswordResetInput("person@example.com"),
            cancellationToken);

        Assert.IsType<SuccessResult>(result);
        Assert.Equal(1, context.TokenGenerator.GenerateCallCount);
        Assert.Equal(32, context.TokenGenerator.GeneratedSizeInBytes);
        Assert.Equal(
            context.TokenGenerator.GeneratedToken.Hash,
            user.PasswordResetTokenHash);
        Assert.NotSame(
            context.TokenGenerator.GeneratedToken.Hash,
            user.PasswordResetTokenHash);
        Assert.Equal(
            context.TimeProvider.UtcNow.AddHours(1),
            user.PasswordResetTokenExpiresAt);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(
            cancellationToken,
            context.UserRepository.SaveCancellationToken);
        Assert.Equal(1, context.EmailService.SendResetPasswordLinkCallCount);
        Assert.Equal("person@example.com", context.EmailService.Email);
        Assert.Equal("Person Example", context.EmailService.FullName);
        Assert.Equal(
            context.TokenGenerator.GeneratedToken.PlainText,
            context.EmailService.PasswordResetToken);
        Assert.Equal(cancellationToken, context.EmailService.CancellationToken);
    }

    [Fact]
    public async Task RequestResetAsync_WhenEmailDeliveryFails_ReturnsSuccess()
    {
        PasswordResetServiceTestContext context = CreateContext();
        context.UserRepository.UserByEmail = CreateUser("person@example.com");
        context.EmailService.SendException =
            new InvalidOperationException("SMTP unavailable");

        Result result = await context.Service.RequestResetAsync(
            new RequestPasswordResetInput("person@example.com"),
            TestContext.Current.CancellationToken);

        Assert.IsType<SuccessResult>(result);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(1, context.EmailService.SendResetPasswordLinkCallCount);
    }

    [Fact]
    public async Task RequestResetAsync_WhenEmailDeliveryIsCanceled_PropagatesCancellation()
    {
        PasswordResetServiceTestContext context = CreateContext();
        context.UserRepository.UserByEmail = CreateUser("person@example.com");
        var cancellationTokenSource = new CancellationTokenSource();
        context.EmailService.SendException =
            new OperationCanceledException(cancellationTokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            context.Service.RequestResetAsync(
                new RequestPasswordResetInput("person@example.com"),
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNullInput_ThrowsArgumentNullException()
    {
        PasswordResetServiceTestContext context = CreateContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.Service.ResetPasswordAsync(
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidInput_ReturnsValidationFailure()
    {
        PasswordResetServiceTestContext context = CreateContext();

        Result result = await context.Service.ResetPasswordAsync(
            new ResetPasswordInput(null, "short", "short"),
            TestContext.Current.CancellationToken);

        ValidationFailureResult failure =
            Assert.IsType<ValidationFailureResult>(result);
        Assert.Contains(
            nameof(ResetPasswordInput.ResetToken),
            failure.ValidationErrors.Keys);
        Assert.Equal(0, context.TokenGenerator.ComputeHashCallCount);
        Assert.Equal(0, context.UserRepository.GetByResetTokenCallCount);
        Assert.Equal(0, context.PasswordHasher.HashCallCount);
        Assert.Equal(0, context.IdentityTransaction.ExecuteCallCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserDoesNotExist_ReturnsInvalidOrExpiredToken()
    {
        PasswordResetServiceTestContext context = CreateContext();

        Result result = await context.Service.ResetPasswordAsync(
            CreateValidResetInput(),
            TestContext.Current.CancellationToken);

        AssertInvalidOrExpiredToken(result);
        Assert.Equal(1, context.TokenGenerator.ComputeHashCallCount);
        Assert.Equal(1, context.UserRepository.GetByResetTokenCallCount);
        Assert.Equal(0, context.PasswordHasher.HashCallCount);
        Assert.Equal(0, context.IdentityTransaction.ExecuteCallCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenExpiresAtCurrentTime_ReturnsInvalidOrExpiredToken()
    {
        PasswordResetServiceTestContext context = CreateContext();
        User user = CreateUserWithResetToken(
            context.TokenGenerator.ComputedHash,
            context.TimeProvider.UtcNow);
        context.UserRepository.UserByResetToken = user;

        Result result = await context.Service.ResetPasswordAsync(
            CreateValidResetInput(),
            TestContext.Current.CancellationToken);

        AssertInvalidOrExpiredToken(result);
        Assert.Equal(0, context.PasswordHasher.HashCallCount);
        Assert.Equal(0, context.IdentityTransaction.ExecuteCallCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenTokenHashDoesNotMatch_ReturnsInvalidOrExpiredToken()
    {
        PasswordResetServiceTestContext context = CreateContext();
        User user = CreateUserWithResetToken(
            CreateHash(200),
            context.TimeProvider.UtcNow.AddMinutes(30));
        context.UserRepository.UserByResetToken = user;

        Result result = await context.Service.ResetPasswordAsync(
            CreateValidResetInput(),
            TestContext.Current.CancellationToken);

        AssertInvalidOrExpiredToken(result);
        Assert.Equal(2, context.TokenGenerator.ComputeHashCallCount);
        Assert.Equal(0, context.PasswordHasher.HashCallCount);
        Assert.Equal(0, context.IdentityTransaction.ExecuteCallCount);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ChangesPasswordAndRevokesSessions()
    {
        PasswordResetServiceTestContext context = CreateContext();
        User user = CreateUserWithResetToken(
            context.TokenGenerator.ComputedHash,
            context.TimeProvider.UtcNow.AddMinutes(30));
        context.UserRepository.UserByResetToken = user;
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result result = await context.Service.ResetPasswordAsync(
            CreateValidResetInput(),
            cancellationToken);

        Assert.IsType<SuccessResult>(result);
        Assert.Equal([71, 72, 73], user.PasswordHash);
        Assert.Equal([81, 82, 83], user.PasswordSalt);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAt);
        Assert.Equal("New secure password", context.PasswordHasher.Password);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(
            cancellationToken,
            context.UserRepository.SaveCancellationToken);
        Assert.Equal(1, context.RefreshTokenService.RevokeAllCallCount);
        Assert.Equal(user.Id, context.RefreshTokenService.RevokedUserId);
        Assert.Equal(
            cancellationToken,
            context.RefreshTokenService.CancellationToken);
        Assert.Equal(1, context.IdentityTransaction.ExecuteCallCount);
        Assert.Equal(
            cancellationToken,
            context.IdentityTransaction.ExecuteCancellationToken);
        Assert.Equal(
            cancellationToken,
            context.IdentityTransaction.OperationCancellationToken);
        Assert.All(context.PasswordHasher.HashBytes, value => Assert.Equal(0, value));
        Assert.All(context.PasswordHasher.SaltBytes, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenSessionRevocationFails_ReturnsFailure()
    {
        PasswordResetServiceTestContext context = CreateContext();
        User user = CreateUserWithResetToken(
            context.TokenGenerator.ComputedHash,
            context.TimeProvider.UtcNow.AddMinutes(30));
        context.UserRepository.UserByResetToken = user;
        context.RefreshTokenService.RevokeAllResult = Result.Failure(
            "RefreshTokens.Unavailable",
            "Refresh-token revocation failed.",
            503);

        Result result = await context.Service.ResetPasswordAsync(
            CreateValidResetInput(),
            TestContext.Current.CancellationToken);

        FailureResult failure = Assert.IsType<FailureResult>(result);
        Assert.Equal("RefreshTokens.Unavailable", failure.Error.Error);
        Assert.Equal(503, failure.Error.StatusCode);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(1, context.RefreshTokenService.RevokeAllCallCount);
        Assert.All(context.PasswordHasher.HashBytes, value => Assert.Equal(0, value));
        Assert.All(context.PasswordHasher.SaltBytes, value => Assert.Equal(0, value));
    }

    private static PasswordResetServiceTestContext CreateContext()
    {
        return new PasswordResetServiceTestContext();
    }

    private static ResetPasswordInput CreateValidResetInput()
    {
        return new ResetPasswordInput(
            "valid-reset-token",
            "New secure password",
            "New secure password");
    }

    private static User CreateUser(string email)
    {
        return User.Create(
            email,
            "Person Example",
            [1, 2, 3],
            [4, 5, 6]);
    }

    private static User CreateUserWithResetToken(
        byte[] tokenHash,
        DateTimeOffset expiresAt)
    {
        User user = CreateUser("person@example.com");
        user.SetPasswordResetToken(
            tokenHash,
            expiresAt.AddHours(-1),
            expiresAt);
        return user;
    }

    private static byte[] CreateHash(byte start)
    {
        byte[] hash = new byte[32];

        for (int index = 0; index < hash.Length; index++)
        {
            hash[index] = (byte)(start + index);
        }

        return hash;
    }

    private static void AssertInvalidOrExpiredToken(Result result)
    {
        FailureResult failure = Assert.IsType<FailureResult>(result);
        Assert.Equal("PasswordReset.InvalidOrExpiredToken", failure.Error.Error);
        Assert.Equal(400, failure.Error.StatusCode);
    }

    private sealed class PasswordResetServiceTestContext
    {
        public PasswordResetServiceTestContext()
        {
            Service = new PasswordResetService(
                new RequestPasswordResetInputValidator(),
                new ResetPasswordInputValidator(),
                UserRepository,
                TokenGenerator,
                PasswordHasher,
                EmailService,
                RefreshTokenService,
                IdentityTransaction,
                TimeProvider,
                NullLogger<PasswordResetService>.Instance);
        }

        public AdjustableTimeProvider TimeProvider { get; } =
            new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));

        public RecordingUserRepository UserRepository { get; } = new();

        public RecordingOpaqueTokenGenerator TokenGenerator { get; } = new();

        public RecordingPasswordHasher PasswordHasher { get; } = new();

        public RecordingEmailService EmailService { get; } = new();

        public RecordingRefreshTokenService RefreshTokenService { get; } = new();

        public RecordingIdentityTransaction IdentityTransaction { get; } = new();

        public PasswordResetService Service { get; }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset utcNow)
        : TimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return UtcNow;
        }
    }

    private sealed class RecordingUserRepository : IUserRepository
    {
        public User? UserByEmail { get; set; }

        public User? UserByResetToken { get; set; }

        public string? RequestedEmail { get; private set; }

        public byte[]? RequestedResetTokenHash { get; private set; }

        public CancellationToken GetByEmailCancellationToken { get; private set; }

        public CancellationToken GetByResetTokenCancellationToken { get; private set; }

        public CancellationToken SaveCancellationToken { get; private set; }

        public int GetByEmailCallCount { get; private set; }

        public int GetByResetTokenCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

        public Task<User?> GetByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
            GetByResetTokenCallCount++;
            RequestedResetTokenHash = tokenHash;
            GetByResetTokenCancellationToken = cancellationToken;
            return Task.FromResult(UserByResetToken);
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
            SaveCallCount++;
            SaveCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingOpaqueTokenGenerator : IOpaqueTokenGenerator
    {
        public GeneratedToken GeneratedToken { get; } = new(
            "generated-reset-token",
            CreateHash(100));

        public byte[] ComputedHash { get; } = CreateHash(100);

        public int GeneratedSizeInBytes { get; private set; }

        public int GenerateCallCount { get; private set; }

        public string? ComputedToken { get; private set; }

        public int ComputeHashCallCount { get; private set; }

        public GeneratedToken Generate(int sizeInBytes)
        {
            GenerateCallCount++;
            GeneratedSizeInBytes = sizeInBytes;
            return GeneratedToken;
        }

        public byte[] ComputeHash(string token)
        {
            ComputeHashCallCount++;
            ComputedToken = token;
            return (byte[])ComputedHash.Clone();
        }
    }

    private sealed class RecordingPasswordHasher : IPasswordHasher
    {
        public byte[] HashBytes { get; } = [71, 72, 73];

        public byte[] SaltBytes { get; } = [81, 82, 83];

        public string? Password { get; private set; }

        public int HashCallCount { get; private set; }

        public HashedPassword Hash(string password)
        {
            HashCallCount++;
            Password = password;
            return new HashedPassword(HashBytes, SaltBytes);
        }

        public bool Verify(string password, byte[] hash, byte[] salt)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingEmailService : IEmailService
    {
        public Exception? SendException { get; set; }

        public string? Email { get; private set; }

        public string? FullName { get; private set; }

        public string? PasswordResetToken { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public int SendResetPasswordLinkCallCount { get; private set; }

        public Task SendVerificationLinkAsync(
            string email,
            string fullName,
            string verificationToken,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SendResetPasswordLinkAsync(
            string email,
            string fullName,
            string passwordResetToken,
            CancellationToken cancellationToken = default)
        {
            SendResetPasswordLinkCallCount++;
            Email = email;
            FullName = fullName;
            PasswordResetToken = passwordResetToken;
            CancellationToken = cancellationToken;

            return SendException is null
                ? Task.CompletedTask
                : Task.FromException(SendException);
        }
    }

    private sealed class RecordingRefreshTokenService : IRefreshTokenService
    {
        public Result RevokeAllResult { get; set; } = Result.Success();

        public Guid RevokedUserId { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public int RevokeAllCallCount { get; private set; }

        public Task<Result<IssuedRefreshToken>> IssueAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
            RevokeAllCallCount++;
            RevokedUserId = userId;
            CancellationToken = cancellationToken;
            return Task.FromResult(RevokeAllResult);
        }
    }

    private sealed class RecordingIdentityTransaction : IIdentityTransaction
    {
        public CancellationToken ExecuteCancellationToken { get; private set; }

        public CancellationToken OperationCancellationToken { get; private set; }

        public int ExecuteCallCount { get; private set; }

        public async Task<Result> ExecuteAsync(
            Func<CancellationToken, Task<Result>> operation,
            CancellationToken cancellationToken = default)
        {
            ExecuteCallCount++;
            ExecuteCancellationToken = cancellationToken;
            OperationCancellationToken = cancellationToken;
            return await operation(cancellationToken);
        }
    }
}
