using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests.Auth;

/// <summary>
/// Represents a request that only requires an email address.
/// Used for operations that only need a user's email identifier.
/// </summary>
public class EmailOnlyRequest
{
    [FromBody]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
