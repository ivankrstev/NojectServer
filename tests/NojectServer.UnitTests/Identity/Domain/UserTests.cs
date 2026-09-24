using NojectServer.Modules.Identity.Domain;

namespace NojectServer.UnitTests.Identity.Domain;

public sealed class UserTests
{
    [Fact]
    public void Create_TrimsValuesNormalizesEmailAndInitializesUnverifiedState()
    {
        byte[] passwordHash = [1, 2, 3];
        byte[] passwordSalt = [4, 5, 6];

        User user = User.Create(
            "  Alice.Example@example.com  ",
            "  Alice Example  ",
            passwordHash,
            passwordSalt);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Alice.Example@example.com", user.Email);
        Assert.Equal("ALICE.EXAMPLE@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("Alice Example", user.FullName);
        Assert.Equal(passwordHash, user.PasswordHash.ToArray());
        Assert.Equal(passwordSalt, user.PasswordSalt.ToArray());
        Assert.Null(user.VerificationTokenHash);
        Assert.Null(user.VerificationTokenExpiresAt);
        Assert.Null(user.VerifiedAt);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAt);
        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.ProtectedTwoFactorSecret);
        Assert.Null(user.LastAcceptedTotpTimeStep);
    }

    [Fact]
    public void Create_ClonesPasswordCredentials()
    {
        byte[] passwordHash = [1, 2, 3];
        byte[] passwordSalt = [4, 5, 6];

        User user = User.Create(
            "person@example.com",
            "Person Example",
            passwordHash,
            passwordSalt);

        passwordHash[0] = 99;
        passwordSalt[0] = 99;

        Assert.Equal([1, 2, 3], user.PasswordHash.ToArray());
        Assert.Equal([4, 5, 6], user.PasswordSalt.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Create_RejectsMissingEmail(string? email)
    {
        Assert.ThrowsAny<ArgumentException>(() => User.Create(
            email!,
            "Person Example",
            [1],
            [2]));
    }

    [Fact]
    public void Create_RejectsEmailLongerThanMaximum()
    {
        string email = new string('a', 255) + "@example.com";

        Assert.Throws<ArgumentException>(() => User.Create(
            email,
            "Person Example",
            [1],
            [2]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Create_RejectsMissingFullName(string? fullName)
    {
        Assert.ThrowsAny<ArgumentException>(() => User.Create(
            "person@example.com",
            fullName!,
            [1],
            [2]));
    }

    [Fact]
    public void Create_RejectsFullNameLongerThanMaximum()
    {
        string fullName = new('a', 51);

        Assert.Throws<ArgumentException>(() => User.Create(
            "person@example.com",
            fullName,
            [1],
            [2]));
    }

    [Fact]
    public void Create_RejectsNullOrEmptyPasswordCredentials()
    {
        Assert.Throws<ArgumentNullException>(() => User.Create(
            "person@example.com",
            "Person Example",
            null!,
            [1]));
        Assert.Throws<ArgumentNullException>(() => User.Create(
            "person@example.com",
            "Person Example",
            [1],
            null!));
        Assert.Throws<ArgumentException>(() => User.Create(
            "person@example.com",
            "Person Example",
            [],
            [1]));
        Assert.Throws<ArgumentException>(() => User.Create(
            "person@example.com",
            "Person Example",
            [1],
            []));
    }

    [Fact]
    public void ChangeEmail_TrimsAndNormalizesEmail()
    {
        User user = CreateUser();

        user.ChangeEmail("  NEW.Email@example.com  ");

        Assert.Equal("NEW.Email@example.com", user.Email);
        Assert.Equal("NEW.EMAIL@EXAMPLE.COM", user.NormalizedEmail);
    }

    [Fact]
    public void ChangeEmail_ClearsVerificationState()
    {
        User user = CreateUser();
        DateTimeOffset issuedAt = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
        user.SetEmailVerificationToken(CreateHash(1), issuedAt, issuedAt.AddHours(1));
        user.MarkAsVerified(issuedAt.AddMinutes(5));

        // A new token can be issued only to an unverified account in the
        // application service, but the aggregate still clears all old state
        // when the email changes.
        user.ChangeEmail("new@example.com");

        Assert.Null(user.VerifiedAt);
        Assert.Null(user.VerificationTokenHash);
        Assert.Null(user.VerificationTokenExpiresAt);
    }

    [Fact]
    public void ChangeEmail_RejectsMissingOrTooLongEmail()
    {
        User user = CreateUser();

        Assert.Throws<ArgumentException>(() => user.ChangeEmail(" "));
        Assert.Throws<ArgumentException>(() => user.ChangeEmail(new string('a', 255)));
    }

    [Fact]
    public void SetEmailVerificationToken_StoresACloneAndExpiration()
    {
        User user = CreateUser();
        byte[] tokenHash = CreateHash(10);
        DateTimeOffset issuedAt = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset expiresAt = issuedAt.AddHours(1);

        user.SetEmailVerificationToken(tokenHash, issuedAt, expiresAt);
        tokenHash[0] = 99;

        Assert.Equal(CreateHash(10), user.VerificationTokenHash?.ToArray());
        Assert.Equal(expiresAt, user.VerificationTokenExpiresAt);
    }

    [Fact]
    public void SetEmailVerificationToken_RejectsInvalidHashOrExpiration()
    {
        User user = CreateUser();
        DateTimeOffset issuedAt = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentNullException>(() => user.SetEmailVerificationToken(
            null!,
            issuedAt,
            issuedAt.AddHours(1)));
        Assert.Throws<ArgumentException>(() => user.SetEmailVerificationToken(
            [1],
            issuedAt,
            issuedAt.AddHours(1)));
        Assert.Throws<ArgumentException>(() => user.SetEmailVerificationToken(
            CreateHash(1),
            issuedAt,
            issuedAt));
        Assert.Throws<ArgumentException>(() => user.SetEmailVerificationToken(
            CreateHash(1),
            issuedAt,
            issuedAt.AddMinutes(-1)));
    }

    [Fact]
    public void MarkAsVerified_RecordsTimestampAndConsumesVerificationToken()
    {
        User user = CreateUser();
        DateTimeOffset issuedAt = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset verifiedAt = issuedAt.AddMinutes(5);
        user.SetEmailVerificationToken(CreateHash(2), issuedAt, issuedAt.AddHours(1));

        user.MarkAsVerified(verifiedAt);

        Assert.Equal(verifiedAt, user.VerifiedAt);
        Assert.Null(user.VerificationTokenHash);
        Assert.Null(user.VerificationTokenExpiresAt);
    }

    [Fact]
    public void SetPasswordResetToken_StoresACloneAndExpiration()
    {
        User user = CreateUser();
        byte[] tokenHash = CreateHash(20);
        DateTimeOffset issuedAt = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset expiresAt = issuedAt.AddHours(1);

        user.SetPasswordResetToken(tokenHash, issuedAt, expiresAt);
        tokenHash[0] = 99;

        Assert.Equal(CreateHash(20), user.PasswordResetTokenHash?.ToArray());
        Assert.Equal(expiresAt, user.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public void SetPasswordResetToken_RejectsInvalidHashOrExpiration()
    {
        User user = CreateUser();
        DateTimeOffset issuedAt = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentNullException>(() => user.SetPasswordResetToken(
            null!,
            issuedAt,
            issuedAt.AddHours(1)));
        Assert.Throws<ArgumentException>(() => user.SetPasswordResetToken(
            [1],
            issuedAt,
            issuedAt.AddHours(1)));
        Assert.Throws<ArgumentException>(() => user.SetPasswordResetToken(
            CreateHash(3),
            issuedAt,
            issuedAt));
        Assert.Throws<ArgumentException>(() => user.SetPasswordResetToken(
            CreateHash(3),
            issuedAt,
            issuedAt.AddMinutes(-1)));
    }

    [Fact]
    public void ChangePassword_ReplacesCredentialsAndClearsResetToken()
    {
        User user = CreateUser();
        DateTimeOffset issuedAt = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
        user.SetPasswordResetToken(CreateHash(30), issuedAt, issuedAt.AddHours(1));
        byte[] passwordHash = [7, 8, 9];
        byte[] passwordSalt = [10, 11, 12];

        user.ChangePassword(passwordHash, passwordSalt);
        passwordHash[0] = 99;
        passwordSalt[0] = 99;

        Assert.Equal([7, 8, 9], user.PasswordHash.ToArray());
        Assert.Equal([10, 11, 12], user.PasswordSalt.ToArray());
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public void ChangePassword_RejectsNullOrEmptyCredentialsWithoutChangingExistingState()
    {
        User user = CreateUser();
        byte[] originalHash = user.PasswordHash.ToArray();
        byte[] originalSalt = user.PasswordSalt.ToArray();

        Assert.Throws<ArgumentNullException>(() => user.ChangePassword(null!, [1]));
        Assert.Throws<ArgumentNullException>(() => user.ChangePassword([1], null!));
        Assert.Throws<ArgumentException>(() => user.ChangePassword([], [1]));
        Assert.Throws<ArgumentException>(() => user.ChangePassword([1], []));

        Assert.Equal(originalHash, user.PasswordHash.ToArray());
        Assert.Equal(originalSalt, user.PasswordSalt.ToArray());
    }

    [Fact]
    public void SetProtectedTwoFactorSecret_StoresACloneAndResetsTfaState()
    {
        User user = CreateUser();
        user.SetProtectedTwoFactorSecret([1, 2, 3]);
        user.EnableTwoFactor();
        user.RecordAcceptedTotpTimeStep(42);
        byte[] replacementSecret = [4, 5, 6];

        user.SetProtectedTwoFactorSecret(replacementSecret);
        replacementSecret[0] = 99;

        Assert.Equal([4, 5, 6], user.ProtectedTwoFactorSecret?.ToArray());
        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.LastAcceptedTotpTimeStep);
    }

    [Fact]
    public void SetProtectedTwoFactorSecret_RejectsNullOrEmptySecret()
    {
        User user = CreateUser();

        Assert.Throws<ArgumentNullException>(() => user.SetProtectedTwoFactorSecret(null!));
        Assert.Throws<ArgumentException>(() => user.SetProtectedTwoFactorSecret([]));
    }

    [Fact]
    public void EnableTwoFactor_RequiresConfiguredSecret()
    {
        User user = CreateUser();

        Assert.Throws<InvalidOperationException>(() => user.EnableTwoFactor());
        Assert.False(user.TwoFactorEnabled);
    }

    [Fact]
    public void EnableTwoFactor_EnablesConfiguredTwoFactor()
    {
        User user = CreateUser();
        user.SetProtectedTwoFactorSecret([1, 2, 3]);

        user.EnableTwoFactor();

        Assert.True(user.TwoFactorEnabled);
        Assert.Equal([1, 2, 3], user.ProtectedTwoFactorSecret?.ToArray());
    }

    [Fact]
    public void DisableTwoFactor_ClearsSecretAndReplayState()
    {
        User user = CreateUser();
        user.SetProtectedTwoFactorSecret([1, 2, 3]);
        user.EnableTwoFactor();
        user.RecordAcceptedTotpTimeStep(42);

        user.DisableTwoFactor();

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.ProtectedTwoFactorSecret);
        Assert.Null(user.LastAcceptedTotpTimeStep);
    }

    [Fact]
    public void RecordAcceptedTotpTimeStep_AllowsZeroAndOnlyMovesForward()
    {
        User user = CreateUser();

        user.RecordAcceptedTotpTimeStep(0);
        user.RecordAcceptedTotpTimeStep(1);

        Assert.Equal(1, user.LastAcceptedTotpTimeStep);
    }

    [Fact]
    public void RecordAcceptedTotpTimeStep_RejectsNegativeAndReplayedSteps()
    {
        User user = CreateUser();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.RecordAcceptedTotpTimeStep(-1));

        user.RecordAcceptedTotpTimeStep(10);

        Assert.Throws<InvalidOperationException>(() =>
            user.RecordAcceptedTotpTimeStep(10));
        Assert.Throws<InvalidOperationException>(() =>
            user.RecordAcceptedTotpTimeStep(9));
        Assert.Equal(10, user.LastAcceptedTotpTimeStep);
    }

    private static User CreateUser()
    {
        return User.Create(
            "person@example.com",
            "Person Example",
            [1, 2, 3],
            [4, 5, 6]);
    }

    private static byte[] CreateHash(byte firstByte)
    {
        byte[] hash = new byte[32];
        hash[0] = firstByte;
        return hash;
    }
}
