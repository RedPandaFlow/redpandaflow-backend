namespace RedPandaFlow.Domain.Entities
{
    public class Card
    {
        public Guid Id { get; set; }
        public Guid ColumnId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int Order { get; set; }
        public bool IsArchived { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        public Column Column { get; set; } = null!;
        public ICollection<CardUser> CardUsers { get; set; } = new List<CardUser>();
    }
}