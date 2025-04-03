using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests.Projects;

/// <summary>
/// Represents a request to create a new project or update an existing project's details.
/// Contains the project name with validation for maximum length.
/// </summary>
public class CreateUpdateProjectRequest
{
    [FromBody]
    [Required]
    [StringLength(50, ErrorMessage = "The Project Name must be a string with a maximum length of 50")]
    public string Name { get; set; } = string.Empty;
}
