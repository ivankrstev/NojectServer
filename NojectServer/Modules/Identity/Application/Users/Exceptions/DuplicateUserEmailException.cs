namespace NojectServer.Modules.Identity.Application.Users.Exceptions;

/// <summary>
/// Represents an attempt to persist a user whose email is already registered.
/// </summary>
public sealed class DuplicateUserEmailException(
    Exception innerException)
    : Exception(
        "An account with this email address already exists.",
        innerException);
