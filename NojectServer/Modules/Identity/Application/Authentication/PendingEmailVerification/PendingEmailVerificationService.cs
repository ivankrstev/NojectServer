using FluentValidation;
using FluentValidation.Results;
using NojectServer.Modules.Identity.Application.Authentication.EmailVerification;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Application.Users.Exceptions;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Utils.ResultPattern;
using NojectServer.Utils.Validation;

namespace NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;

/// <summary>
/// Coordinates the restricted account-management workflow for users whose
/// email address has not yet been verified.
/// </summary>
internal sealed class PendingEmailVerificationService(
    IValidator<ChangePendingEmailInput> changeEmailValidator,
    IValidator<DeletePendingAccountInput> deleteAccountValidator,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailVerificationService emailVerificationService,
    ILogger<PendingEmailVerificationService> logger)
    : IPendingEmailVerificationService
{
    private readonly IValidator<ChangePendingEmailInput> _changeEmailValidator =
        changeEmailValidator;
    private readonly IValidator<DeletePendingAccountInput> _deleteAccountValidator =
        deleteAccountValidator;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IEmailVerificationService _emailVerificationService =
        emailVerificationService;
    private readonly ILogger<PendingEmailVerificationService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result> ResendAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        User? user = await GetPendingUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                PendingEmailVerificationErrors.WorkflowNotAvailable);
        }

        return await _emailVerificationService.RequestVerificationAsync(
            new RequestEmailVerificationInput(user.Email),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> ChangeEmailAsync(
        Guid userId,
        ChangePendingEmailInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _changeEmailValidator.ValidateAsync(input, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure(
                validationResult.ToErrorDictionary());
        }

        User? user = await GetPendingUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                PendingEmailVerificationErrors.WorkflowNotAvailable);
        }

        string newEmail = input.NewEmail!.Trim();

        if (string.Equals(
                user.Email,
                newEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                PendingEmailVerificationErrors.EmailUnchanged);
        }

        if (await _userRepository.ExistsByEmailAsync(
                newEmail,
                cancellationToken))
        {
            return Result.Failure(
                PendingEmailVerificationErrors.EmailAlreadyRegistered);
        }

        user.ChangeEmail(newEmail);

        try
        {
            await _userRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateUserEmailException exception)
        {
            _logger.LogWarning(
                exception,
                "Pending email change failed for user {UserId}; concurrent duplicate email.",
                user.Id);

            return Result.Failure(
                PendingEmailVerificationErrors.EmailAlreadyRegistered);
        }

        return await _emailVerificationService.RequestVerificationAsync(
            new RequestEmailVerificationInput(user.Email),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAccountAsync(
        Guid userId,
        DeletePendingAccountInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidationResult validationResult =
            await _deleteAccountValidator.ValidateAsync(input, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.ValidationFailure(
                validationResult.ToErrorDictionary());
        }

        User? user = await GetPendingUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                PendingEmailVerificationErrors.WorkflowNotAvailable);
        }

        if (!_passwordHasher.Verify(
                input.Password!,
                user.PasswordHash.Span,
                user.PasswordSalt.Span))
        {
            return Result.Failure(
                PendingEmailVerificationErrors.InvalidPassword);
        }

        bool accountDeleted = await _userRepository.DeleteUnverifiedAsync(
            user.Id,
            cancellationToken);

        if (!accountDeleted)
        {
            return Result.Failure(
                PendingEmailVerificationErrors.WorkflowNotAvailable);
        }

        return Result.Success();
    }

    private async Task<User?> GetPendingUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        User? user = await _userRepository.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null || user.VerifiedAt is not null)
        {
            return null;
        }

        return user;
    }
}
