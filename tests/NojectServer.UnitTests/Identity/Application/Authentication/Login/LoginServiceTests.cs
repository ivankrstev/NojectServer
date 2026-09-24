using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.UnitTests.Identity.Application.Authentication.Login;

public sealed class LoginServiceTests
{
    [Fact]
    public async Task LoginAsync_WithNullInput_ThrowsArgumentNullException()
    {
        LoginServiceTestContext context = CreateContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.Service.LoginAsync(
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidInput_ReturnsValidationFailure()
    {
        LoginServiceTestContext context = CreateContext();

        Result<LoginResult> result = await context.Service.LoginAsync(
            new LoginInput(null, "password"),
            TestContext.Current.CancellationToken);

        ValidationFailureResult<LoginResult> failure =
            Assert.IsType<ValidationFailureResult<LoginResult>>(result);
        Assert.Contains(nameof(LoginInput.Email), failure.ValidationErrors.Keys);
        Assert.Equal(0, context.UserRepository.GetByEmailCallCount);
        Assert.Equal(0, context.PasswordHasher.HashCallCount);
        Assert.Equal(0, context.PasswordHasher.VerifyCallCount);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsInvalidCredentialsAndHashesDummyPassword()
    {
        LoginServiceTestContext context = CreateContext();

        Result<LoginResult> result = await context.Service.LoginAsync(
            new LoginInput("missing@example.com", "correct password"),
            TestContext.Current.CancellationToken);

        FailureResult<LoginResult> failure =
            Assert.IsType<FailureResult<LoginResult>>(result);
        Assert.Equal("Login.InvalidCredentials", failure.Error.Error);
        Assert.Equal("missing@example.com", context.UserRepository.RequestedEmail);
        Assert.Equal("correct password", context.PasswordHasher.HashedPassword);
        Assert.Equal(1, context.PasswordHasher.HashCallCount);
        Assert.Equal(0, context.PasswordHasher.VerifyCallCount);
        Assert.All(context.PasswordHasher.DummyHash, value => Assert.Equal(0, value));
        Assert.All(context.PasswordHasher.DummySalt, value => Assert.Equal(0, value));
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsInvalidCredentialsWithoutIssuingTokens()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser("person@example.com", verified: true);
        context.UserRepository.UserByEmail = user;
        context.PasswordHasher.VerifyResult = false;

        Result<LoginResult> result = await context.Service.LoginAsync(
            new LoginInput("person@example.com", "wrong password"),
            TestContext.Current.CancellationToken);

        FailureResult<LoginResult> failure =
            Assert.IsType<FailureResult<LoginResult>>(result);
        Assert.Equal("Login.InvalidCredentials", failure.Error.Error);
        Assert.Equal("wrong password", context.PasswordHasher.VerifiedPassword);
        Assert.Equal(user.PasswordHash.ToArray(), context.PasswordHasher.VerifiedHash);
        Assert.Equal(user.PasswordSalt.ToArray(), context.PasswordHasher.VerifiedSalt);
        Assert.Equal(0, context.PasswordHasher.HashCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateTfaTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ReturnsPendingVerificationChallenge()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser("person@example.com", verified: false);
        context.UserRepository.UserByEmail = user;
        context.PasswordHasher.VerifyResult = true;

        Result<LoginResult> result = await context.Service.LoginAsync(
            new LoginInput("person@example.com", "correct password"),
            TestContext.Current.CancellationToken);

        SuccessResult<LoginResult> success =
            Assert.IsType<SuccessResult<LoginResult>>(result);
        EmailVerificationRequiredLoginResult pending =
            Assert.IsType<EmailVerificationRequiredLoginResult>(success.Value);
        Assert.Equal(
            context.JwtTokenService.PendingVerificationToken.Token,
            pending.PendingVerificationToken);
        Assert.Equal(
            context.JwtTokenService.PendingVerificationToken.ExpiresAt,
            pending.ExpiresAt);
        Assert.Equal("p***n@example.com", pending.MaskedEmail);
        Assert.Equal(
            user.Id,
            context.JwtTokenService.PendingVerificationTokenUserId);
        Assert.Equal(1, context.JwtTokenService.CreatePendingVerificationTokenCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateTfaTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task LoginAsync_WithVerifiedEmailAndDisabledTwoFactor_ReturnsAuthenticatedSession()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser("person@example.com", verified: true);
        context.UserRepository.UserByEmail = user;
        context.PasswordHasher.VerifyResult = true;
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result<LoginResult> result = await context.Service.LoginAsync(
            new LoginInput("person@example.com", "correct password"),
            cancellationToken);

        SuccessResult<LoginResult> success =
            Assert.IsType<SuccessResult<LoginResult>>(result);
        AuthenticatedLoginResult authenticated =
            Assert.IsType<AuthenticatedLoginResult>(success.Value);
        Assert.Equal(
            context.JwtTokenService.AccessToken.Token,
            authenticated.AccessToken);
        Assert.Equal(
            context.JwtTokenService.AccessToken.ExpiresAt,
            authenticated.AccessTokenExpiresAt);
        Assert.Equal(
            context.RefreshTokenService.IssuedToken.Token,
            authenticated.RefreshToken);
        Assert.Equal(
            context.RefreshTokenService.IssuedToken.ExpiresAt,
            authenticated.RefreshTokenExpiresAt);
        Assert.Equal(user.Id, context.JwtTokenService.AccessTokenUserId);
        Assert.Equal(user.Id, context.RefreshTokenService.IssuedForUserId);
        Assert.Equal(
            cancellationToken,
            context.UserRepository.GetByEmailCancellationToken);
        Assert.Equal(
            cancellationToken,
            context.RefreshTokenService.IssueCancellationToken);
        Assert.Equal(1, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateTfaTokenCallCount);
        Assert.Equal(1, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task LoginAsync_WithVerifiedEmailAndEnabledTwoFactor_ReturnsTwoFactorChallenge()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser(
            "person@example.com",
            verified: true,
            twoFactorEnabled: true);
        context.UserRepository.UserByEmail = user;
        context.PasswordHasher.VerifyResult = true;

        Result<LoginResult> result = await context.Service.LoginAsync(
            new LoginInput("person@example.com", "correct password"),
            TestContext.Current.CancellationToken);

        SuccessResult<LoginResult> success =
            Assert.IsType<SuccessResult<LoginResult>>(result);
        TwoFactorRequiredLoginResult challenge =
            Assert.IsType<TwoFactorRequiredLoginResult>(success.Value);
        Assert.Equal(context.JwtTokenService.TfaToken.Token, challenge.TwoFactorToken);
        Assert.Equal(context.JwtTokenService.TfaToken.ExpiresAt, challenge.ExpiresAt);
        Assert.Equal(user.Id, context.JwtTokenService.TfaTokenUserId);
        Assert.Equal(1, context.JwtTokenService.CreateTfaTokenCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task LoginAsync_WhenRefreshTokenIssuanceFails_ReturnsAuthenticationCompletionFailure()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser("person@example.com", verified: true);
        context.UserRepository.UserByEmail = user;
        context.PasswordHasher.VerifyResult = true;
        context.RefreshTokenService.IssueResult = Result.Failure<IssuedRefreshToken>(
            "RefreshToken.Failed",
            "Refresh token issuance failed.",
            500);

        Result<LoginResult> result = await context.Service.LoginAsync(
            new LoginInput("person@example.com", "correct password"),
            TestContext.Current.CancellationToken);

        FailureResult<LoginResult> failure =
            Assert.IsType<FailureResult<LoginResult>>(result);
        Assert.Equal(
            "Login.AuthenticationCompletionFailed",
            failure.Error.Error);
        Assert.Equal(1, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(1, context.RefreshTokenService.IssueCallCount);
    }

    private static LoginServiceTestContext CreateContext()
    {
        return new LoginServiceTestContext();
    }

    private static User CreateUser(
        string email,
        bool verified,
        bool twoFactorEnabled = false)
    {
        User user = User.Create(
            email,
            "Person Example",
            [1, 2, 3],
            [4, 5, 6]);

        if (verified)
        {
            user.MarkAsVerified(
                new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));
        }

        if (twoFactorEnabled)
        {
            user.SetProtectedTwoFactorSecret([7, 8, 9]);
            user.EnableTwoFactor();
        }

        return user;
    }
}
