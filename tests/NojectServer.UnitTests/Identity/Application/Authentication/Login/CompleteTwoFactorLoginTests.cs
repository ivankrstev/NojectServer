using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Application.JwtTokens;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.UnitTests.Identity.Application.Authentication.Login;

public sealed class CompleteTwoFactorLoginTests
{
    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WithNullInput_ThrowsArgumentNullException()
    {
        LoginServiceTestContext context = CreateContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            context.Service.CompleteTwoFactorLoginAsync(
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WithInvalidInput_ReturnsValidationFailure()
    {
        LoginServiceTestContext context = CreateContext();

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                new CompleteTwoFactorLoginInput(null, "123456"),
                TestContext.Current.CancellationToken);

        ValidationFailureResult<AuthenticatedLoginResult> failure =
            Assert.IsType<ValidationFailureResult<AuthenticatedLoginResult>>(result);
        Assert.Contains(
            nameof(CompleteTwoFactorLoginInput.TfaToken),
            failure.ValidationErrors.Keys);
        Assert.Equal(0, context.JwtTokenService.ValidateTfaTokenCallCount);
        Assert.Equal(0, context.TwoFactorAuthService.ValidateCodeCallCount);
        Assert.Equal(0, context.UserRepository.GetByIdCallCount);
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WithInvalidToken_ReturnsInvalidChallenge()
    {
        LoginServiceTestContext context = CreateContext();
        context.JwtTokenService.TfaValidationResult = Result.Failure<TfaTokenClaims>(
            "Token.Invalid",
            "The token is invalid.",
            401);

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken);

        FailureResult<AuthenticatedLoginResult> failure =
            Assert.IsType<FailureResult<AuthenticatedLoginResult>>(result);
        Assert.Equal(
            "Login.InvalidOrExpiredTwoFactorChallenge",
            failure.Error.Error);
        Assert.Equal("header.payload.signature", context.JwtTokenService.ValidatedTfaToken);
        Assert.Equal(1, context.JwtTokenService.ValidateTfaTokenCallCount);
        Assert.Equal(0, context.TwoFactorAuthService.ValidateCodeCallCount);
        Assert.Equal(0, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WithInvalidCode_ReturnsTwoFactorFailure()
    {
        LoginServiceTestContext context = CreateContext();
        Guid userId = Guid.NewGuid();
        context.JwtTokenService.TfaValidationResult = Result.Success(
            new TfaTokenClaims(userId));
        context.TwoFactorAuthService.CodeValidationResult = Result.Failure(
            "TwoFactor.InvalidCode",
            "The code is invalid.",
            401);

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken);

        FailureResult<AuthenticatedLoginResult> failure =
            Assert.IsType<FailureResult<AuthenticatedLoginResult>>(result);
        Assert.Equal(
            "Login.TwoFactorAuthenticationFailed",
            failure.Error.Error);
        Assert.Equal(userId, context.TwoFactorAuthService.ValidatedUserId);
        Assert.Equal("123456", context.TwoFactorAuthService.ValidatedCode);
        Assert.Equal(1, context.TwoFactorAuthService.ValidateCodeCallCount);
        Assert.Equal(0, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WhenUserIsMissing_ReturnsInvalidChallenge()
    {
        LoginServiceTestContext context = CreateContext();
        Guid userId = Guid.NewGuid();
        context.JwtTokenService.TfaValidationResult = Result.Success(
            new TfaTokenClaims(userId));
        context.UserRepository.UserById = null;

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken);

        FailureResult<AuthenticatedLoginResult> failure =
            Assert.IsType<FailureResult<AuthenticatedLoginResult>>(result);
        Assert.Equal(
            "Login.InvalidOrExpiredTwoFactorChallenge",
            failure.Error.Error);
        Assert.Equal(userId, context.UserRepository.RequestedUserId);
        Assert.Equal(1, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WhenEmailBecameUnverified_ReturnsInvalidChallenge()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        context.JwtTokenService.TfaValidationResult = Result.Success(
            new TfaTokenClaims(user.Id));
        context.UserRepository.UserById = user;
        context.TwoFactorAuthService.CodeValidationResult = Result.Success();
        user.ChangeEmail("changed@example.com");

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken);

        FailureResult<AuthenticatedLoginResult> failure =
            Assert.IsType<FailureResult<AuthenticatedLoginResult>>(result);
        Assert.Equal(
            "Login.InvalidOrExpiredTwoFactorChallenge",
            failure.Error.Error);
        Assert.Equal(1, context.TwoFactorAuthService.ValidateCodeCallCount);
        Assert.Equal(1, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WhenTwoFactorIsDisabled_ReturnsInvalidChallenge()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: false);
        context.JwtTokenService.TfaValidationResult = Result.Success(
            new TfaTokenClaims(user.Id));
        context.UserRepository.UserById = user;
        context.TwoFactorAuthService.CodeValidationResult = Result.Success();

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken);

        FailureResult<AuthenticatedLoginResult> failure =
            Assert.IsType<FailureResult<AuthenticatedLoginResult>>(result);
        Assert.Equal(
            "Login.InvalidOrExpiredTwoFactorChallenge",
            failure.Error.Error);
        Assert.Equal(1, context.UserRepository.GetByIdCallCount);
        Assert.Equal(0, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(0, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WithValidCodeAndEligibleUser_ReturnsAuthenticatedSession()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        context.JwtTokenService.TfaValidationResult = Result.Success(
            new TfaTokenClaims(user.Id));
        context.UserRepository.UserById = user;
        context.TwoFactorAuthService.CodeValidationResult = Result.Success();
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                CreateValidInput(),
                cancellationToken);

        SuccessResult<AuthenticatedLoginResult> success =
            Assert.IsType<SuccessResult<AuthenticatedLoginResult>>(result);
        Assert.Equal(
            context.JwtTokenService.AccessToken.Token,
            success.Value.AccessToken);
        Assert.Equal(
            context.JwtTokenService.AccessToken.ExpiresAt,
            success.Value.AccessTokenExpiresAt);
        Assert.Equal(
            context.RefreshTokenService.IssuedToken.Token,
            success.Value.RefreshToken);
        Assert.Equal(
            context.RefreshTokenService.IssuedToken.ExpiresAt,
            success.Value.RefreshTokenExpiresAt);
        Assert.Equal(user.Id, context.TwoFactorAuthService.ValidatedUserId);
        Assert.Equal(user.Id, context.UserRepository.RequestedUserId);
        Assert.Equal(user.Id, context.JwtTokenService.AccessTokenUserId);
        Assert.Equal(user.Id, context.RefreshTokenService.IssuedForUserId);
        Assert.Equal(
            cancellationToken,
            context.TwoFactorAuthService.ValidationCancellationToken);
        Assert.Equal(
            cancellationToken,
            context.UserRepository.GetByIdCancellationToken);
        Assert.Equal(
            cancellationToken,
            context.RefreshTokenService.IssueCancellationToken);
        Assert.Equal(1, context.JwtTokenService.CreateAccessTokenCallCount);
        Assert.Equal(1, context.RefreshTokenService.IssueCallCount);
    }

    [Fact]
    public async Task CompleteTwoFactorLoginAsync_WhenRefreshTokenIssuanceFails_ReturnsAuthenticationCompletionFailure()
    {
        LoginServiceTestContext context = CreateContext();
        User user = CreateUser(twoFactorEnabled: true);
        context.JwtTokenService.TfaValidationResult = Result.Success(
            new TfaTokenClaims(user.Id));
        context.UserRepository.UserById = user;
        context.RefreshTokenService.IssueResult = Result.Failure<IssuedRefreshToken>(
            "RefreshToken.Failed",
            "Refresh token issuance failed.",
            500);

        Result<AuthenticatedLoginResult> result =
            await context.Service.CompleteTwoFactorLoginAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken);

        FailureResult<AuthenticatedLoginResult> failure =
            Assert.IsType<FailureResult<AuthenticatedLoginResult>>(result);
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

    private static CompleteTwoFactorLoginInput CreateValidInput()
    {
        return new CompleteTwoFactorLoginInput(
            "header.payload.signature",
            "123456");
    }

    private static User CreateUser(bool twoFactorEnabled)
    {
        User user = User.Create(
            "person@example.com",
            "Person Example",
            [1, 2, 3],
            [4, 5, 6]);
        user.MarkAsVerified(
            new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));

        if (twoFactorEnabled)
        {
            user.SetProtectedTwoFactorSecret([7, 8, 9]);
            user.EnableTwoFactor();
        }

        return user;
    }
}
