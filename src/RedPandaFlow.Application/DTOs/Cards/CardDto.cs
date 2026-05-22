namespace RedPandaFlow.Application.DTOs
{
    public class CardDto
    {
        public Guid Id { get; set; }
        public Guid ColumnId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public int Order { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}