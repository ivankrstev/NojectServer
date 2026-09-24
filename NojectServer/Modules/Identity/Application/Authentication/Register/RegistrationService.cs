using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Application.Users.Exceptions;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;
using NojectServer.Utils.Validation;

namespace NojectServer.Modules.Identity.Application.Authentication.Register;

/// <summary>
/// Registers new users, hashing their password and enforcing that email
/// addresses remain unique across the identity system.
/// </summary>
internal sealed class RegistrationService(
    IValidator<RegisterInput> registerInputValidator,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<RegistrationService> logger) : IRegistrationService
{
    private readonly IValidator<RegisterInput> _registerInputValidator = registerInputValidator;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ILogger<RegistrationService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<RegisteredUser>> RegisterAsync(
        RegisterInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _registerInputValidator.ValidateAsync(
                input,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure<RegisteredUser>(
                validationResult.ToErrorDictionary());
        }

        string email = input.Email!.Trim();
        string fullName = input.FullName!.Trim();

        bool emailExists = await _userRepository.ExistsByEmailAsync(
            email,
            cancellationToken);

        if (emailExists)
        {
            return Result.Failure<RegisteredUser>(
                RegisterErrors.EmailAlreadyRegistered);
        }

        HashedPassword hashedPassword = _passwordHasher.Hash(input.Password!);

        try
        {
            var user = User.Create(
                email: email,
                fullName: fullName,
                passwordHash: hashedPassword.Hash,
                passwordSalt: hashedPassword.Salt);

            _userRepository.Add(user);

            try
            {
                await _userRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateUserEmailException exception)
            {
                // The preliminary email check is not authoritative because another
                // registration can create the account before this request is persisted.
                _logger.LogWarning(
                    exception,
                    "Registration save failed due to concurrent duplicate registration.");

                return Result.Failure<RegisteredUser>(
                    RegisterErrors.EmailAlreadyRegistered);
            }

            return Result.Success(
                new RegisteredUser(
                    UserId: user.Id,
                    Email: user.Email,
                    FullName: user.FullName));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hashedPassword.Hash);
            CryptographicOperations.ZeroMemory(hashedPassword.Salt);
        }
    }
}
