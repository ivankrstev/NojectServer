using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.Register;

/// <summary>
/// Registers new users in the identity system.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Validates the registration input and creates a new user account.
    /// </summary>
    /// <param name="input">
    /// The registration information supplied by the user.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A result containing information about the newly registered user.
    /// </returns>
    Task<Result<RegisteredUser>> RegisterAsync(
        RegisterInput input,
        CancellationToken cancellationToken = default);
}
