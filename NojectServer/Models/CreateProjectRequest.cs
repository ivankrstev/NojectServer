using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models
{
    public class CreateProjectRequest
    {
        [Required]
        [StringLength(50, ErrorMessage = "The Project Name must be a string with a maximum length of 50")]
        public string Name { get; set; } = string.Empty;
    }
}