namespace RedPandaFlow.Domain.Entities
{
    public class Board
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        public Workspace Workspace { get; set; } = null!;
        public ICollection<Column> Columns { get; set; } = new List<Column>();
    }
}