using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Repositories.Base;
using NojectServer.Repositories.Interfaces;
using Task = NojectServer.Models.Task;

namespace NojectServer.Repositories.Implementations;

public class TaskRepository(DataContext dataContext)
    : GenericRepository<Task>(dataContext), ITaskRepository
{
    public async Task<Task?> GetByProjectAndTaskIdAsync(Guid projectId, int taskId)
    {
        return await _dbSet.FindAsync(taskId, projectId);
    }

    public async Task<IEnumerable<Task>> GetByProjectIdAsync(Guid projectId)
    {
        return await _dbSet.Where(t => t.ProjectId == projectId).ToListAsync();
    }
}
