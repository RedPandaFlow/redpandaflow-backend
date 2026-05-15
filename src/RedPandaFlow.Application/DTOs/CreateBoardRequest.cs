using System.ComponentModel.DataAnnotations;

namespace RedPandaFlow.Application.DTOs
{
    public class CreateBoardRequest
    {
        [Required(ErrorMessage = "Le titre est obligatoire")]
        [MaxLength(25, ErrorMessage = "Le titre est trop long")]
        public string Title { get; set; } = string.Empty;
    }
}