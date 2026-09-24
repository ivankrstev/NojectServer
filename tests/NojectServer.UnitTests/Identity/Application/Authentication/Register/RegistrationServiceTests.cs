using Microsoft.Extensions.Logging.Abstractions;
using NojectServer.Modules.Identity.Application.Authentication.Register;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Application.Users.Exceptions;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.UnitTests.Identity.Application.Authentication.Register;

public sealed class RegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithNullInput_ThrowsArgumentNullException()
    {
        RegistrationService service = CreateService(
            new RecordingUserRepository(),
            new RecordingPasswordHasher());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.RegisterAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidInput_ReturnsValidationFailure()
    {
        var userRepository = new RecordingUserRepository();
        var passwordHasher = new RecordingPasswordHasher();
        RegistrationService service = CreateService(
            userRepository,
            passwordHasher);

        Result<RegisteredUser> result = await service.RegisterAsync(
            new RegisterInput(null, "Person Example", "password", "password"),
            TestContext.Current.CancellationToken);

        ValidationFailureResult<RegisteredUser> failure =
            Assert.IsType<ValidationFailureResult<RegisteredUser>>(result);
        Assert.Contains(
            nameof(RegisterInput.Email),
            failure.ValidationErrors.Keys);
        Assert.Null(userRepository.CheckedEmail);
        Assert.Null(userRepository.AddedUser);
        Assert.Null(passwordHasher.Password);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsConflictWithoutHashing()
    {
        var userRepository = new RecordingUserRepository
        {
            EmailExists = true
        };
        var passwordHasher = new RecordingPasswordHasher();
        RegistrationService service = CreateService(
            userRepository,
            passwordHasher);

        Result<RegisteredUser> result = await service.RegisterAsync(
            CreateValidInput(),
            TestContext.Current.CancellationToken);

        FailureResult<RegisteredUser> failure =
            Assert.IsType<FailureResult<RegisteredUser>>(result);
        Assert.Equal("Register.EmailAlreadyRegistered", failure.Error.Error);
        Assert.Equal(409, failure.Error.StatusCode);
        Assert.Equal("person@example.com", userRepository.CheckedEmail);
        Assert.Null(userRepository.AddedUser);
        Assert.Equal(0, userRepository.SaveCallCount);
        Assert.Null(passwordHasher.Password);
    }

    [Fact]
    public async Task RegisterAsync_WithValidInput_CreatesAndPersistsUser()
    {
        var userRepository = new RecordingUserRepository();
        var passwordHasher = new RecordingPasswordHasher(
            hash: [11, 12, 13],
            salt: [21, 22, 23]);
        RegistrationService service = CreateService(
            userRepository,
            passwordHasher);
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Result<RegisteredUser> result = await service.RegisterAsync(
            new RegisterInput(
                "person@example.com",
                "  Person Example  ",
                "correct password",
                "correct password"),
            cancellationToken);

        SuccessResult<RegisteredUser> success =
            Assert.IsType<SuccessResult<RegisteredUser>>(result);
        User addedUser = Assert.IsType<User>(userRepository.AddedUser);
        Assert.Equal(addedUser.Id, success.Value.UserId);
        Assert.Equal("person@example.com", success.Value.Email);
        Assert.Equal("Person Example", success.Value.FullName);
        Assert.Equal("person@example.com", addedUser.Email);
        Assert.Equal("PERSON@EXAMPLE.COM", addedUser.NormalizedEmail);
        Assert.Equal("Person Example", addedUser.FullName);
        Assert.Equal([11, 12, 13], addedUser.PasswordHash.ToArray());
        Assert.Equal([21, 22, 23], addedUser.PasswordSalt.ToArray());
        Assert.Equal("correct password", passwordHasher.Password);
        Assert.Equal(
            cancellationToken,
            userRepository.ExistsCancellationToken);
        Assert.Equal(
            cancellationToken,
            userRepository.SaveCancellationToken);
        Assert.Equal(1, userRepository.SaveCallCount);
        Assert.All(passwordHasher.HashBytes, value => Assert.Equal(0, value));
        Assert.All(passwordHasher.SaltBytes, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task RegisterAsync_WhenSaveDetectsDuplicate_ReturnsConflictAndClearsHash()
    {
        var userRepository = new RecordingUserRepository
        {
            SaveException = new DuplicateUserEmailException(
                new InvalidOperationException("unique constraint"))
        };
        var passwordHasher = new RecordingPasswordHasher(
            hash: [31, 32],
            salt: [41, 42]);
        RegistrationService service = CreateService(
            userRepository,
            passwordHasher);

        Result<RegisteredUser> result = await service.RegisterAsync(
            CreateValidInput(),
            TestContext.Current.CancellationToken);

        FailureResult<RegisteredUser> failure =
            Assert.IsType<FailureResult<RegisteredUser>>(result);
        Assert.Equal("Register.EmailAlreadyRegistered", failure.Error.Error);
        Assert.NotNull(userRepository.AddedUser);
        Assert.Equal(1, userRepository.SaveCallCount);
        Assert.All(passwordHasher.HashBytes, value => Assert.Equal(0, value));
        Assert.All(passwordHasher.SaltBytes, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task RegisterAsync_WhenAddingUserFails_ClearsHashAndPropagatesException()
    {
        var userRepository = new RecordingUserRepository
        {
            AddException = new InvalidOperationException("tracking failed")
        };
        var passwordHasher = new RecordingPasswordHasher(
            hash: [51, 52],
            salt: [61, 62]);
        RegistrationService service = CreateService(
            userRepository,
            passwordHasher);

        InvalidOperationException exception = await Assert.ThrowsAsync<
            InvalidOperationException>(() => service.RegisterAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken));

        Assert.Equal("tracking failed", exception.Message);
        Assert.Equal(0, userRepository.SaveCallCount);
        Assert.All(passwordHasher.HashBytes, value => Assert.Equal(0, value));
        Assert.All(passwordHasher.SaltBytes, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task RegisterAsync_WhenSaveFailsWithUnexpectedException_ClearsHashAndPropagatesException()
    {
        var userRepository = new RecordingUserRepository
        {
            SaveException = new InvalidOperationException("database unavailable")
        };
        var passwordHasher = new RecordingPasswordHasher(
            hash: [71, 72],
            salt: [81, 82]);
        RegistrationService service = CreateService(
            userRepository,
            passwordHasher);

        InvalidOperationException exception = await Assert.ThrowsAsync<
            InvalidOperationException>(() => service.RegisterAsync(
                CreateValidInput(),
                TestContext.Current.CancellationToken));

        Assert.Equal("database unavailable", exception.Message);
        Assert.All(passwordHasher.HashBytes, value => Assert.Equal(0, value));
        Assert.All(passwordHasher.SaltBytes, value => Assert.Equal(0, value));
    }

    private static RegistrationService CreateService(
        RecordingUserRepository userRepository,
        RecordingPasswordHasher passwordHasher)
    {
        return new RegistrationService(
            new RegisterInputValidator(),
            userRepository,
            passwordHasher,
            NullLogger<RegistrationService>.Instance);
    }

    private static RegisterInput CreateValidInput()
    {
        return new RegisterInput(
            "person@example.com",
            "Person Example",
            "correct password",
            "correct password");
    }

    private sealed class RecordingUserRepository : IUserRepository
    {
        public bool EmailExists { get; init; }

        public Exception? AddException { get; init; }

        public Exception? SaveException { get; init; }

        public string? CheckedEmail { get; private set; }

        public CancellationToken ExistsCancellationToken { get; private set; }

        public CancellationToken SaveCancellationToken { get; private set; }

        public User? AddedUser { get; private set; }

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
            CheckedEmail = email;
            ExistsCancellationToken = cancellationToken;
            return Task.FromResult(EmailExists);
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
            if (AddException is not null)
            {
                throw AddException;
            }

            AddedUser = user;
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

    private sealed class RecordingPasswordHasher : IPasswordHasher
    {
        public RecordingPasswordHasher(
            byte[]? hash = null,
            byte[]? salt = null)
        {
            HashBytes = hash ?? [1, 2, 3];
            SaltBytes = salt ?? [4, 5, 6];
        }

        public byte[] HashBytes { get; }

        public byte[] SaltBytes { get; }

        public string? Password { get; private set; }

        public HashedPassword Hash(string password)
        {
            Password = password;
            return new HashedPassword(HashBytes, SaltBytes);
        }

        public bool Verify(
            string password,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> salt)
        {
            throw new NotSupportedException();
        }
    }
}
