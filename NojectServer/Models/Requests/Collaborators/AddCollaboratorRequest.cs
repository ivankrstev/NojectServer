using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests.Collaborators;

/// <summary>
/// Represents a request to add a collaborator to a project.
/// Contains the unique identifier of the user to be added as a collaborator.
/// </summary>
public class AddCollaboratorRequest
{
    [FromBody]
    [Required]
    public string UserId { get; set; } = string.Empty;
}
