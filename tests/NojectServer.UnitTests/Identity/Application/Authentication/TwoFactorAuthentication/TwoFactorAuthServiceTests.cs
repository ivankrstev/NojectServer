using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.UnitTests.Identity.Application.Authentication.TwoFactorAuthentication;

public sealed class TwoFactorAuthServiceTests
{
    [Fact]
    public async Task GenerateSetupCodeAsync_WithEmptyUserId_ReturnsInvalidUserId()
    {
        TwoFactorAuthTestContext context = CreateContext();

        Result<TwoFactorSetup> result =
            await context.Service.GenerateSetupCodeAsync(
                Guid.Empty,
                TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidUserId);
        Assert.Equal(0, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.TotpService.GenerateSecretCallCount);
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_WhenUserIsMissing_ReturnsUserNotFound()
    {
        TwoFactorAuthTestContext context = CreateContext();
        Guid userId = Guid.NewGuid();

        Result<TwoFactorSetup> result =
            await context.Service.GenerateSetupCodeAsync(
                userId,
                TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.UserNotFound);
        Assert.Equal(userId, context.UserRepository.RequestedUserId);
        Assert.Equal(1, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.TotpService.GenerateSecretCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_WhenTwoFactorIsAlreadyEnabled_ReturnsAlreadyEnabled()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        context.UserRepository.UserById = user;

        Result<TwoFactorSetup> result =
            await context.Service.GenerateSetupCodeAsync(
                user.Id,
                TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.AlreadyEnabled);
        Assert.Equal(0, context.TotpService.GenerateSecretCallCount);
        Assert.Equal(0, context.SecretProtector.ProtectCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_WithValidUser_PersistsSecretAndReturnsSetupDetails()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: false);
        context.UserRepository.UserById = user;
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result<TwoFactorSetup> result =
            await context.Service.GenerateSetupCodeAsync(
                user.Id,
                cancellationToken);

        SuccessResult<TwoFactorSetup> success =
            Assert.IsType<SuccessResult<TwoFactorSetup>>(result);
        Assert.Equal("manual-key", success.Value.ManualKey);
        Assert.Equal("otpauth://totp/Person%20Example", success.Value.ProvisioningUri);
        Assert.Equal(1, context.TotpService.GenerateSecretCallCount);
        Assert.Equal(1, context.SecretProtector.ProtectCallCount);
        Assert.Equal(user.Id, context.SecretProtector.ProtectedUserId);
        Assert.Equal(
            context.TotpService.GeneratedSecretBeforeCleanup,
            context.SecretProtector.ProtectInput);
        Assert.Equal(user.Email, context.TotpService.ProvisioningAccountName);
        Assert.Equal(
            context.TotpService.GeneratedSecretBeforeCleanup,
            context.TotpService.EncodedSecret);
        Assert.Equal(
            context.TotpService.GeneratedSecretBeforeCleanup,
            context.TotpService.ProvisioningSecret);
        Assert.Equal([91, 92, 93], user.ProtectedTwoFactorSecret);
        Assert.NotSame(
            context.SecretProtector.ProtectedSecret,
            user.ProtectedTwoFactorSecret);
        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.LastAcceptedTotpTimeStep);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(cancellationToken, context.UserRepository.SaveCancellationToken);
        Assert.All(
            context.TotpService.GeneratedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task GenerateSetupCodeAsync_WhenSaveDetectsConcurrency_ReturnsConcurrentSetup()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: false);
        context.UserRepository.UserById = user;
        context.UserRepository.SaveException =
            new DbUpdateConcurrencyException("Concurrent setup.");

        Result<TwoFactorSetup> result =
            await context.Service.GenerateSetupCodeAsync(
                user.Id,
                TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.ConcurrentSetup);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.All(
            context.TotpService.GeneratedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task EnableAsync_WithEmptyUserId_ReturnsInvalidUserId()
    {
        TwoFactorAuthTestContext context = CreateContext();

        Result result = await context.Service.EnableAsync(
            Guid.Empty,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidUserId);
        Assert.Equal(0, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.TotpService.TryValidateCodeCallCount);
    }

    [Fact]
    public async Task EnableAsync_WhenUserIsMissing_ReturnsUserNotFound()
    {
        TwoFactorAuthTestContext context = CreateContext();
        Guid userId = Guid.NewGuid();

        Result result = await context.Service.EnableAsync(
            userId,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.UserNotFound);
        Assert.Equal(0, context.TotpService.TryValidateCodeCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task EnableAsync_WhenTwoFactorIsAlreadyEnabled_ReturnsAlreadyEnabled()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        context.UserRepository.UserById = user;

        Result result = await context.Service.EnableAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.AlreadyEnabled);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.TotpService.TryValidateCodeCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task EnableAsync_WhenSecretIsNotConfigured_ReturnsNotConfigured()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: false);
        context.UserRepository.UserById = user;

        Result result = await context.Service.EnableAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.NotConfigured);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.TotpService.TryValidateCodeCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task EnableAsync_WithInvalidCode_ReturnsInvalidCodeWithoutChangingUser()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateConfiguredUser(twoFactorEnabled: false);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = false;

        Result result = await context.Service.EnableAsync(
            user.Id,
            "wrong-code",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidCode);
        Assert.Equal(user.Id, context.SecretProtector.UnprotectedUserId);
        Assert.Equal(1, context.SecretProtector.UnprotectCallCount);
        Assert.Equal("wrong-code", context.TotpService.ValidatedCode);
        Assert.Equal([21, 22, 23], context.TotpService.ValidatedSecret);
        Assert.Equal(1, context.TotpService.TryValidateCodeCallCount);
        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.LastAcceptedTotpTimeStep);
        Assert.Equal([7, 8, 9], user.ProtectedTwoFactorSecret);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task EnableAsync_WhenCodeWasAlreadyConsumed_ReturnsInvalidCodeWithoutSaving()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateConfiguredUser(twoFactorEnabled: false);
        user.RecordAcceptedTotpTimeStep(42);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = true;
        context.TotpService.MatchedTimeStep = 42;

        Result result = await context.Service.EnableAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidCode);
        Assert.False(user.TwoFactorEnabled);
        Assert.Equal(42, user.LastAcceptedTotpTimeStep);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task EnableAsync_WithValidCode_EnablesTwoFactorAndRecordsTimeStep()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateConfiguredUser(twoFactorEnabled: false);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = true;
        context.TotpService.MatchedTimeStep = 42;
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result result = await context.Service.EnableAsync(
            user.Id,
            "123456",
            cancellationToken);

        Assert.IsType<SuccessResult>(result);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal(42, user.LastAcceptedTotpTimeStep);
        Assert.Equal([7, 8, 9], user.ProtectedTwoFactorSecret);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(cancellationToken, context.UserRepository.SaveCancellationToken);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task EnableAsync_WhenSaveDetectsConcurrency_ReturnsInvalidCode()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateConfiguredUser(twoFactorEnabled: false);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = true;
        context.UserRepository.SaveException =
            new DbUpdateConcurrencyException("Concurrent code use.");

        Result result = await context.Service.EnableAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidCode);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task DisableAsync_WithEmptyUserId_ReturnsInvalidUserId()
    {
        TwoFactorAuthTestContext context = CreateContext();

        Result result = await context.Service.DisableAsync(
            Guid.Empty,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidUserId);
        Assert.Equal(0, context.UserRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task DisableAsync_WhenUserIsMissing_ReturnsUserNotFound()
    {
        TwoFactorAuthTestContext context = CreateContext();

        Result result = await context.Service.DisableAsync(
            Guid.NewGuid(),
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.UserNotFound);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task DisableAsync_WhenTwoFactorIsNotEnabled_ReturnsNotEnabled()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: false);
        user.SetProtectedTwoFactorSecret([7, 8, 9]);
        context.UserRepository.UserById = user;

        Result result = await context.Service.DisableAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.NotEnabled);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task DisableAsync_WhenTwoFactorIsDisabledWithoutSecret_ReturnsNotEnabled()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        user.DisableTwoFactor();
        context.UserRepository.UserById = user;

        Result result = await context.Service.DisableAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.NotEnabled);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task DisableAsync_WithInvalidCode_ReturnsInvalidCodeWithoutChangingUser()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        user.RecordAcceptedTotpTimeStep(10);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = false;

        Result result = await context.Service.DisableAsync(
            user.Id,
            "wrong-code",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidCode);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal([7, 8, 9], user.ProtectedTwoFactorSecret);
        Assert.Equal(10, user.LastAcceptedTotpTimeStep);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task DisableAsync_WithValidCode_DisablesTwoFactorAndClearsSecret()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        user.RecordAcceptedTotpTimeStep(10);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = true;
        context.TotpService.MatchedTimeStep = 11;
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result result = await context.Service.DisableAsync(
            user.Id,
            "123456",
            cancellationToken);

        Assert.IsType<SuccessResult>(result);
        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.ProtectedTwoFactorSecret);
        Assert.Null(user.LastAcceptedTotpTimeStep);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(cancellationToken, context.UserRepository.SaveCancellationToken);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task ValidateCodeAsync_WithEmptyUserId_ReturnsInvalidUserId()
    {
        TwoFactorAuthTestContext context = CreateContext();

        Result result = await context.Service.ValidateCodeAsync(
            Guid.Empty,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidUserId);
        Assert.Equal(0, context.UserRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenUserIsMissing_ReturnsUserNotFound()
    {
        TwoFactorAuthTestContext context = CreateContext();

        Result result = await context.Service.ValidateCodeAsync(
            Guid.NewGuid(),
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.UserNotFound);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenTwoFactorIsNotEnabled_ReturnsNotEnabled()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: false);
        user.SetProtectedTwoFactorSecret([7, 8, 9]);
        context.UserRepository.UserById = user;

        Result result = await context.Service.ValidateCodeAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.NotEnabled);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenTwoFactorIsDisabledWithoutSecret_ReturnsNotEnabled()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        user.DisableTwoFactor();
        context.UserRepository.UserById = user;

        Result result = await context.Service.ValidateCodeAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.NotEnabled);
        Assert.Equal(0, context.SecretProtector.UnprotectCallCount);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
    }

    [Fact]
    public async Task ValidateCodeAsync_WithInvalidCode_ReturnsInvalidCodeWithoutSaving()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        user.RecordAcceptedTotpTimeStep(10);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = false;

        Result result = await context.Service.ValidateCodeAsync(
            user.Id,
            "wrong-code",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidCode);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal([7, 8, 9], user.ProtectedTwoFactorSecret);
        Assert.Equal(10, user.LastAcceptedTotpTimeStep);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenCodeWasAlreadyConsumed_ReturnsInvalidCodeWithoutSaving()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        user.RecordAcceptedTotpTimeStep(42);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = true;
        context.TotpService.MatchedTimeStep = 41;

        Result result = await context.Service.ValidateCodeAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidCode);
        Assert.Equal(42, user.LastAcceptedTotpTimeStep);
        Assert.Equal(0, context.UserRepository.SaveCallCount);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task ValidateCodeAsync_WithValidCodeRecordsTimeStepAndSaves()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        user.RecordAcceptedTotpTimeStep(10);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = true;
        context.TotpService.MatchedTimeStep = 11;
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result result = await context.Service.ValidateCodeAsync(
            user.Id,
            "123456",
            cancellationToken);

        Assert.IsType<SuccessResult>(result);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal(11, user.LastAcceptedTotpTimeStep);
        Assert.Equal([7, 8, 9], user.ProtectedTwoFactorSecret);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.Equal(cancellationToken, context.UserRepository.SaveCancellationToken);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenSaveDetectsConcurrency_ReturnsInvalidCode()
    {
        TwoFactorAuthTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        context.UserRepository.UserById = user;
        context.TotpService.TryValidateCodeResult = true;
        context.TotpService.MatchedTimeStep = 42;
        context.UserRepository.SaveException =
            new DbUpdateConcurrencyException("Concurrent code use.");

        Result result = await context.Service.ValidateCodeAsync(
            user.Id,
            "123456",
            TestContext.Current.CancellationToken);

        AssertFailure(result, TwoFactorAuthErrors.InvalidCode);
        Assert.Equal(42, user.LastAcceptedTotpTimeStep);
        Assert.Equal(1, context.UserRepository.SaveCallCount);
        Assert.All(
            context.SecretProtector.UnprotectedSecret,
            value => Assert.Equal(0, value));
    }

    private static TwoFactorAuthTestContext CreateContext()
    {
        return new TwoFactorAuthTestContext();
    }

    private static User CreateUser(bool twoFactorEnabled)
    {
        User user = User.Create(
            "person@example.com",
            "Person Example",
            [1, 2, 3],
            [4, 5, 6]);

        if (twoFactorEnabled)
        {
            user.SetProtectedTwoFactorSecret([7, 8, 9]);
            user.EnableTwoFactor();
        }

        return user;
    }

    private static User CreateConfiguredUser(bool twoFactorEnabled)
    {
        User user = CreateUser(twoFactorEnabled: false);
        user.SetProtectedTwoFactorSecret([7, 8, 9]);

        if (twoFactorEnabled)
        {
            user.EnableTwoFactor();
        }

        return user;
    }

    private static void AssertFailure(Result result, ErrorDetails expectedError)
    {
        FailureResult failure = Assert.IsType<FailureResult>(result);
        Assert.Equal(expectedError.Error, failure.Error.Error);
        Assert.Equal(expectedError.Message, failure.Error.Message);
        Assert.Equal(expectedError.StatusCode, failure.Error.StatusCode);
    }

    private static void AssertFailure<T>(
        Result<T> result,
        ErrorDetails expectedError)
    {
        FailureResult<T> failure = Assert.IsType<FailureResult<T>>(result);
        Assert.Equal(expectedError.Error, failure.Error.Error);
        Assert.Equal(expectedError.Message, failure.Error.Message);
        Assert.Equal(expectedError.StatusCode, failure.Error.StatusCode);
    }

    private sealed class TwoFactorAuthTestContext
    {
        public TwoFactorAuthTestContext()
        {
            Service = new TwoFactorAuthService(
                UserRepository,
                TotpService,
                SecretProtector,
                NullLogger<TwoFactorAuthService>.Instance);
        }

        public RecordingUserRepository UserRepository { get; } = new();

        public RecordingTotpService TotpService { get; } = new();

        public RecordingSecretProtector SecretProtector { get; } = new();

        public TwoFactorAuthService Service { get; }
    }

    private sealed class RecordingUserRepository : IUserRepository
    {
        public User? UserById { get; set; }

        public Exception? SaveException { get; set; }

        public Guid RequestedUserId { get; private set; }

        public CancellationToken GetByIdCancellationToken { get; private set; }

        public CancellationToken SaveCancellationToken { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

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
            throw new NotSupportedException();
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
            SaveCallCount++;
            SaveCancellationToken = cancellationToken;

            return SaveException is null
                ? Task.CompletedTask
                : Task.FromException(SaveException);
        }
    }

    private sealed class RecordingTotpService : ITotpService
    {
        public byte[] GeneratedSecret { get; } = [11, 12, 13, 14];

        public byte[] GeneratedSecretBeforeCleanup { get; } = [11, 12, 13, 14];

        public byte[] EncodedSecret { get; private set; } = [];

        public byte[] ProvisioningSecret { get; private set; } = [];

        public string? ProvisioningAccountName { get; private set; }

        public bool TryValidateCodeResult { get; set; }

        public long MatchedTimeStep { get; set; } = 42;

        public string? ValidatedCode { get; private set; }

        public byte[] ValidatedSecret { get; private set; } = [];

        public int GenerateSecretCallCount { get; private set; }

        public int TryValidateCodeCallCount { get; private set; }

        public byte[] GenerateSecret()
        {
            GenerateSecretCallCount++;
            return GeneratedSecret;
        }

        public string EncodeSecret(byte[] secret)
        {
            EncodedSecret = (byte[])secret.Clone();
            return "manual-key";
        }

        public string CreateProvisioningUri(byte[] secret, string accountName)
        {
            ProvisioningSecret = (byte[])secret.Clone();
            ProvisioningAccountName = accountName;
            return "otpauth://totp/Person%20Example";
        }

        public bool TryValidateCode(
            byte[] secret,
            string? code,
            out long matchedTimeStep)
        {
            TryValidateCodeCallCount++;
            ValidatedSecret = (byte[])secret.Clone();
            ValidatedCode = code;
            matchedTimeStep = MatchedTimeStep;
            return TryValidateCodeResult;
        }
    }

    private sealed class RecordingSecretProtector : ITwoFactorSecretProtector
    {
        public byte[] ProtectedSecret { get; } = [91, 92, 93];

        public byte[] UnprotectedSecret { get; } = [21, 22, 23];

        public byte[] ProtectInput { get; private set; } = [];

        public Guid ProtectedUserId { get; private set; }

        public Guid UnprotectedUserId { get; private set; }

        public int ProtectCallCount { get; private set; }

        public int UnprotectCallCount { get; private set; }

        public byte[] Protect(Guid userId, byte[] secret)
        {
            ProtectCallCount++;
            ProtectedUserId = userId;
            ProtectInput = (byte[])secret.Clone();
            return ProtectedSecret;
        }

        public byte[] Unprotect(Guid userId, byte[] protectedSecret)
        {
            UnprotectCallCount++;
            UnprotectedUserId = userId;
            return UnprotectedSecret;
        }
    }
}
