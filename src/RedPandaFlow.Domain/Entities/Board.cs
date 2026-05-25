namespace RedPandaFlow.Domain.Entities
{
    public class Board
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Workspace Workspace { get; set; } = null!;
        public User Owner { get; set; } = null!;
        public ICollection<Column> Columns { get; set; } = new List<Column>();
        public ICollection<BoardUser> Members { get; set; } = new List<BoardUser>();
        public ICollection<Label> Labels { get; set; } = new List<Label>();
    }
}