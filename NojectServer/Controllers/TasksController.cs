using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Middlewares;
using NojectServer.Utils;

namespace NojectServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Authorize]
    [ServiceFilter(typeof(VerifyProjectAccess))]
    public class TasksController : ControllerBase
    {
        private readonly DataContext _dataContext;

        public TasksController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetTasks(Guid id)
        {
            int? first_task = await _dataContext.Projects.Where(p => p.Id == id).AsNoTracking().Select(p => p.FirstTask).FirstOrDefaultAsync();
            var tasks = await _dataContext.Tasks.Where(t => t.ProjectId == id).AsNoTrackingWithIdentityResolution().ToArrayAsync();
            tasks.OrderTasks(first_task);
            return Ok(new { tasks });
        }
    }
}