namespace RedPandaFlow.Domain.Entities
{
    public class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User Owner { get; set; } = null!;
        public ICollection<WorkspaceUser> Members { get; set; } = new List<WorkspaceUser>();
    }
}
