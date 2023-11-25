using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Middlewares;
using NojectServer.Models;
using NojectServer.ResponseMessages;
using System.Security.Claims;

namespace NojectServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CollaboratorsController : ControllerBase
    {
        private readonly DataContext _dataContext;

        public CollaboratorsController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpPost("{id}")]
        [ServiceFilter(typeof(VerifyProjectOwnership))]
        [ProducesResponseType(typeof(SuccessMessage), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Add(Guid id, AddCollaboratorRequest request)
        {
            string UserEmailToAdd = request.UserId;
            string ProjectOwnerEmail = User.FindFirst(ClaimTypes.Name)?.Value!;
            if (UserEmailToAdd == ProjectOwnerEmail)
            {
                return BadRequest(new
                {
                    error = "Error adding collaborator",
                    message = "You cannot add yourself as collaborator"
                });
            }
            if (!await _dataContext.Users.AnyAsync(u => u.Email == UserEmailToAdd))
            {
                return NotFound(new
                {
                    error = "User not found",
                    message = "The specified user doesn't exist"
                });
            }
            if (await _dataContext.Collaborators.AnyAsync(c => c.ProjectId == id && c.CollaboratorId == UserEmailToAdd))
            {
                return NotFound(new
                {
                    error = "Collaborator Already Exists",
                    message = "This collaborator is already associated with this project"
                });
            }
            await _dataContext.AddAsync(new Collaborator { ProjectId = id, CollaboratorId = UserEmailToAdd });
            await _dataContext.SaveChangesAsync();
            return Ok(new { message = $"Successfully added {UserEmailToAdd} as a collaborator" });
        }
    }
}