using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NojectServer.Middlewares;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Utils;

namespace NojectServer.Controllers;

[Route("[controller]")]
[ApiController]
[Produces("application/json")]
[Authorize]
[ServiceFilter(typeof(VerifyProjectAccess))]
public class TasksController(IUnitOfWork unitOfWork) : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    [HttpGet("{id}")]
    public async Task<ActionResult> GetTasks(Guid id)
    {
        int? first_task = await _unitOfWork.Projects.Query().Where(p => p.Id == id).AsNoTracking().Select(p => p.FirstTask).FirstOrDefaultAsync();
        var unorderedTasks = await _unitOfWork.Tasks.FindAsync(t => t.ProjectId == id);
        Models.Task[] tasks = [.. unorderedTasks];
        tasks.OrderTasks(first_task);
        return Ok(new { tasks });
    }
}
