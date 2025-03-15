using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NojectServer.Middlewares;
using NojectServer.Models.Requests;
using NojectServer.ResponseMessages;
using NojectServer.Services.Collaborators.Interfaces;
using NojectServer.Utils.ResultPattern;
using System.Security.Claims;

namespace NojectServer.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(VerifyProjectOwnership))]
public class CollaboratorsController(ICollaboratorsService collaboratorsService) : ControllerBase
{
    private readonly ICollaboratorsService _collaboratorsService = collaboratorsService;

    [HttpPost("{id}")]
    [ProducesResponseType(typeof(SuccessMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(Guid id, AddCollaboratorRequest request)
    {
        string projectOwnerEmail = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _collaboratorsService.AddCollaboratorAsync(id, request.UserId, projectOwnerEmail);

        return result switch
        {
            SuccessResult<string> success => Ok(success.Value),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpGet("search/{id}")]
    [ProducesResponseType(typeof(SuccessMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<string>>> Search(Guid id, [FromQuery, BindRequired] string userToFind)
    {
        string projectOwnerEmail = User.FindFirst(ClaimTypes.Name)?.Value!;
        var result = await _collaboratorsService.SearchCollaboratorsAsync(id, userToFind, projectOwnerEmail);

        return result switch
        {
            SuccessResult<List<string>> success => Ok(new { users = success.Value }),
            FailureResult<List<string>> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SuccessMessage), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetAll(Guid id)
    {
        var result = await _collaboratorsService.GetAllCollaboratorsAsync(id);

        return result switch
        {
            SuccessResult<List<string>> success => Ok(new { collaborators = success.Value }),
            FailureResult<List<string>> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(SuccessMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid id, [FromQuery, BindRequired] string userToRemove)
    {
        var result = await _collaboratorsService.RemoveCollaboratorAsync(id, userToRemove);

        return result switch
        {
            SuccessResult<string> success => Ok(new SuccessMessage { Message = success.Value }),
            FailureResult<string> failure => StatusCode(failure.Error.StatusCode,
                new { error = failure.Error.Error, message = failure.Error.Message }),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }
}
