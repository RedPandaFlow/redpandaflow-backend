using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Domain.Entities
{
    public class WorkspaceUser
    {
        public Guid WorkspaceId { get; set; }
        public Guid UserId { get; set; }
        public WorkspaceRole Role { get; set; }

        public Workspace Workspace { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
