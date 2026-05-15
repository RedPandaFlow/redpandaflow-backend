using System.ComponentModel.DataAnnotations;

namespace RedPandaFlow.Application.DTOs
{
    public class UpdateColumnRequest
    {
        [Required]
        [MaxLength(25)]
        public string Title { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
    }
}