using System.ComponentModel.DataAnnotations;

namespace RedPandaFlow.Application.DTOs
{
    public class CreateBoardRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(25, ErrorMessage = "Title is too long. Maximum length is 25 characters.")]
        public string Title { get; set; } = string.Empty;
    }
}