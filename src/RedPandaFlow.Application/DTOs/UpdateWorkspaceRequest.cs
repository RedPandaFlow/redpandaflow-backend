using System.ComponentModel.DataAnnotations;

namespace RedPandaFlow.Application.DTOs
{
    public class UpdateWorkspaceRequest
    {
        [Required]
        [StringLength(25, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
