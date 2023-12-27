using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Hubs;
using NojectServer.Middlewares;
using NojectServer.Models;
using NojectServer.Models.Requests;
using NojectServer.ResponseMessages;
using StackExchange.Redis;
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
        private readonly IHubContext<SharedProjectsHub> _hubContext;
        private readonly IDatabase _redisDb;

        public CollaboratorsController(DataContext dataContext, IHubContext<SharedProjectsHub> hubContext, IConnectionMultiplexer connectionMultiplexer)
        {
            _dataContext = dataContext;
            _hubContext = hubContext;
            _redisDb = connectionMultiplexer.GetDatabase();
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
            List<string> conns = _redisDb.SetMembersAsync($"sharedprojects:{UserEmailToAdd}").GetAwaiter().GetResult().Select(rv => (string)rv!).ToList();
            await _hubContext.Clients.Clients(conns).SendAsync("NewSharedProject", $"You were added as collabarotor to the project {id}");
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

        [ProducesResponseType(typeof(SuccessMessage), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorWithDetailedMessage), StatusCodes.Status404NotFound)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(Guid id, [FromQuery, BindRequired] string userToRemove)
        {
            var deletedRows = await _dataContext.Collaborators.Where(c => c.ProjectId == id && c.CollaboratorId == userToRemove).ExecuteDeleteAsync();
            if (deletedRows == 0)
            {
                return NotFound(new
                {
                    error = "Collaborator not found",
                    message = "The specified collaborator doesn't exist"
                });
            }
            List<string> conns = _redisDb.SetMembersAsync($"sharedprojects:{userToRemove}").GetAwaiter().GetResult().Select(rv => (string)rv!).ToList();
            await _hubContext.Clients.Clients(conns).SendAsync("RemovedSharedProject", $"You were removed as collaborator from the project {id}");
            return Ok(new { message = $"Successfully removed {userToRemove} as a collaborator" });
        }
    }
}