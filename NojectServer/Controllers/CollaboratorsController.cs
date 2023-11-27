using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    [ServiceFilter(typeof(VerifyProjectOwnership))]
    public class CollaboratorsController : ControllerBase
    {
        private readonly DataContext _dataContext;

        public CollaboratorsController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpPost("{id}")]
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

        [HttpGet("search/{id}")]
        public async Task<ActionResult<List<string>>> Search(Guid id, [FromQuery, BindRequired] string userToFind)
        {
            string ProjectOwnerEmail = User.FindFirst(ClaimTypes.Name)?.Value!;
            var query = from user in _dataContext.Users
                        join collaborator in _dataContext.Collaborators
                        on new { UserId = user.Email, ProjectId = id } equals new
                        {
                            UserId = collaborator.CollaboratorId,
                            collaborator.ProjectId
                        } into gj
                        from subCollaborator in gj.DefaultIfEmpty()
                        where subCollaborator == null && user.Email != ProjectOwnerEmail && user.Email.StartsWith(userToFind)
                        select user.Email;
            return Ok(new { users = await query.ToListAsync() });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<List<string>>> GetAll(Guid id)
        {
            List<string> collaborators = await _dataContext.Collaborators.Where(c => c.ProjectId == id).Select(c => c.CollaboratorId).ToListAsync();
            return Ok(new { collaborators });
        }
    }
}