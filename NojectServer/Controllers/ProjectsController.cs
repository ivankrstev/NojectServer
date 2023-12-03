﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Middlewares;
using NojectServer.Models;
using NojectServer.Models.Requests;
using System.Security.Claims;

namespace NojectServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly DataContext _dataContext;

        public ProjectsController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpPost("", Name = "Create a Project")]
        public async Task<ActionResult<Project>> Create(AddUpdateProjectRequest request)
        {
            GenerateColors(out string color, out string backgroundColor);
            Project project = new()
            {
                Name = request.Name,
                BackgroundColor = backgroundColor,
                Color = color,
                CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value!
            };
            _dataContext.Add(project);
            await _dataContext.SaveChangesAsync();
            return Created(nameof(Project), project);
        }

        [HttpGet("", Name = "Get all own projects")]
        public async Task<ActionResult<List<Project>>> GetOwnProjects()
        {
            var user = User.FindFirst(ClaimTypes.Name)?.Value!;
            List<Project> projects = await _dataContext.Projects.Where(p => p.CreatedBy == user).ToListAsync();
            return Ok(new { projects });
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(VerifyProjectOwnership))]
        public async Task<ActionResult> UpdateProjectName(Guid id, AddUpdateProjectRequest request)
        {
            var project = await _dataContext.Projects.Where(p => p.Id == id).SingleOrDefaultAsync();
            project!.Name = request.Name;
            await _dataContext.SaveChangesAsync();
            return Ok(new { message = $"Project with ID {id} successfully updated" });
        }

        [HttpDelete("{id}", Name = "DeleteById")]
        [ServiceFilter(typeof(VerifyProjectOwnership))]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _dataContext.Projects.Where(p => p.Id == id).ExecuteDeleteAsync();
            return Ok(new { message = $"Project with ID {id} successfully deleted" });
        }

        private static void GenerateColors(out string color, out string backgroundColor)
        {
            string letters = "0123456789ABCDEF";
            backgroundColor = "#";
            for (int i = 0; i < 6; i++) backgroundColor += letters[new Random().Next(0, 16)];
            int red = int.Parse(backgroundColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            int green = int.Parse(backgroundColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            int blue = int.Parse(backgroundColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            int yiq = (red * 299 + green * 587 + blue * 114) / 1000;
            color = yiq >= 128 ? "#000" : "#FFF";
        }
    }
}