using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests
{
    public class EmailOnlyRequest
    {
        [FromBody, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}