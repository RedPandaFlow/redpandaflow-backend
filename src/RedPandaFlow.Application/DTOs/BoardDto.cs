using RedPandaFlow.Domain.Enums;
namespace RedPandaFlow.Application.DTOs
{
    public class BoardDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ColumnDto> Columns { get; set; } = new List<ColumnDto>();
    }

    public class BoardMemberDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Role Role { get; set; }
        public bool IsOwner { get; set; }
    }
}