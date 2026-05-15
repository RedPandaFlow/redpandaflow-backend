using System.ComponentModel.DataAnnotations;

namespace RedPandaFlow.Application.DTOs
{
    public class UpdateBoardRequest
    {
        [Required(ErrorMessage = "Le titre est obligatoire")]
        [MaxLength(25)]
        public string Title { get; set; } = string.Empty;
    }
}