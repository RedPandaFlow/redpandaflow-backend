using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Application.DTOs
{
    public class WorkspaceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public WorkspaceRole CurrentUserRole { get; set; }
        public int MemberCount { get; set; }
    }

    public class WorkspaceMemberDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public WorkspaceRole Role { get; set; }
        public bool IsOwner { get; set; }
    }
}
