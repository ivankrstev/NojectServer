﻿using Microsoft.IdentityModel.Tokens;

namespace NojectServer.Services.Auth.Interfaces;

/// <summary>
/// Defines the contract for JWT token generation and validation services.
/// This interface provides methods to create various token types for authentication
/// and authorization purposes in the application.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a JWT access token for the specified user email.
    /// Access tokens are short-lived tokens used for API authentication.
    /// </summary>
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    string CreateAccessToken(string email);

    /// <summary>
    /// Creates a JWT refresh token for the specified user email.
    /// Refresh tokens are long-lived tokens used to obtain new access tokens.
    /// </summary>
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    string CreateRefreshToken(string email);

    /// <summary>
    /// Creates a JWT token for two-factor authentication (TFA) purposes.
    /// TFA tokens are short-lived tokens used during the two-factor authentication flow.
    /// </summary>
    /// <param name="email">The email address of the user to create the token for</param>
    /// <returns>A signed JWT token string</returns>
    string CreateTfaToken(string email);

    /// <summary>
    /// Provides the validation parameters required to verify TFA tokens.
    /// These parameters define how a TFA token should be validated, including
    /// issuer, audience, lifetime, and signing key verification.
    /// </summary>
    /// <returns>TokenValidationParameters for TFA token validation</returns>
    TokenValidationParameters GetTfaTokenValidationParameters();
}
