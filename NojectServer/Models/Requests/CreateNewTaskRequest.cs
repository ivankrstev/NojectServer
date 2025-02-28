using Microsoft.AspNetCore.Mvc;

namespace NojectServer.Models.Requests;

public class CreateNewTaskRequest
{
    // Get the prev task id, so the new task can be added after that one
    [FromBody]
    public int? Prev { get; set; }
}