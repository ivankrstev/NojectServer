using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests
{
    public class AddCollaboratorRequest
    {
        [FromBody, Required]
        public string UserId { get; set; } = string.Empty;
    }
}