using System.ComponentModel.DataAnnotations;

namespace RedPandaFlow.Application.DTOs
{
    public class CreateCardRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(25, ErrorMessage = "Title is too long. Maximum length is 25 characters.")]
        public string Title { get; set; } = string.Empty;
        [MaxLength(500, ErrorMessage = "Description is too long. Maximum length is 500 characters.")]
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
    }
}