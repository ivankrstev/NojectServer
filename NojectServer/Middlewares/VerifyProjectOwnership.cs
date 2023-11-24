using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using System.Security.Claims;

namespace NojectServer.Middlewares
{
    [AttributeUsage(AttributeTargets.Method)]
    public class VerifyProjectOwnership : Attribute, IAsyncResourceFilter
    {
        private readonly DataContext _dataContext;

        public VerifyProjectOwnership(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            string UserEmail = context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value!;
            if (context.RouteData.Values.TryGetValue("id", out var IdParam) && Guid.TryParse(IdParam!.ToString(), out Guid ParsedId))
            {
                if (!await _dataContext.Projects.AnyAsync(p => p.Id == ParsedId && p.CreatedBy == UserEmail))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Access denied",
                        message = "You do not have permission to access this project"
                    });
                }
                else await next();
                return;
            }
            context.HttpContext.Response.StatusCode = 400;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Invalid project id",
                message = "The provided project id is not a valid GUID"
            });
            return;
        }
    }
}