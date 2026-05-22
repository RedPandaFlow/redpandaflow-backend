using System.ComponentModel.DataAnnotations;
using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Application.DTOs
{
    public class InviteMemberRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        public Role Role { get; set; } = Role.Member;
    }
}
