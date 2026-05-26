using System.ComponentModel.DataAnnotations;

namespace RedPandaFlow.Application.DTOs
{
    public class UpdateColumnRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(25, ErrorMessage = "Title is too long. Maximum length is 25 characters.")]
        public string Title { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
    }
}