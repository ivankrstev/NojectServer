using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Middlewares;
using NojectServer.Models.Requests;
using NojectServer.Utils;
using System.Security.Claims;

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
        private static readonly SemaphoreSlim semaphore = new(1, 1);

        public TasksController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpPost("{id}")]
        public async Task<ActionResult> Create(Guid id, CreateNewTaskRequest? request)
        {
            int? prev = request?.Prev;
            var user = User.FindFirst(ClaimTypes.Name)?.Value!;
            using var transaction = await _dataContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                await semaphore.WaitAsync();
                var project = await _dataContext.Projects
                    .Where(p => p.Id == id).FirstAsync();
                var maxId = await _dataContext.Tasks
                    .Where(t => t.ProjectId == id)
                    .Select(t => (int?)t.Id)
                    .MaxAsync() ?? 0;
                var prevTask = prev != null ? await _dataContext.Tasks.Where(t => t.Id == prev && t.ProjectId == id).FirstOrDefaultAsync() : null;
                Models.Task task = new()
                {
                    Id = maxId + 1,
                    ProjectId = id,
                    CreatedBy = user
                };
                _dataContext.Add(task);
                project.FirstTask ??= task.Id;
                _dataContext.SaveChanges();
                // Change prev, next task pointers
                task.Next = prevTask?.Next;
                if (prevTask != null) prevTask!.Next = task.Id;
                else
                {
                    var lastTask = await _dataContext.Tasks.Where(t => t.Id != task.Id && t.Next == null && t.ProjectId == id).FirstOrDefaultAsync();
                    if (lastTask != null) lastTask!.Next = task.Id;
                }
                _dataContext.SaveChanges();
                transaction.Commit();
                semaphore.Release();
                return Ok(task);
            }
            catch (Exception ex)
            {
                semaphore.Release();
                await transaction.RollbackAsync();
                return StatusCode(500, ex.InnerException);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetTasks(Guid id)
        {
            int? first_task = await _dataContext.Projects.Where(p => p.Id == id).Select(p => p.FirstTask).FirstOrDefaultAsync();
            var unorderedTasks = await _dataContext.Tasks.Where(t => t.ProjectId == id).ToArrayAsync();
            List<Models.Task> tasks = TasksHandler.OrderTasks(unorderedTasks, first_task);
            return Ok(new { tasks });
        }
    }
}