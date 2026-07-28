namespace NojectServer.Modules.Identity.Application.Tokens;

/// <summary>
/// Contains an opaque token for delivery and its hash for persistence.
/// </summary>
/// <param name="PlainText">
/// The token value returned to the intended recipient. It should not be persisted.
/// </param>
/// <param name="Hash">
/// The token hash used for persistence and subsequent validation.
/// </param>
public sealed record GeneratedToken(
    string PlainText,
    byte[] Hash);
