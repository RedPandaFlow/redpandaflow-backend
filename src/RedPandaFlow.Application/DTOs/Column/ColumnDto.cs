namespace RedPandaFlow.Application.DTOs
{
    public class ColumnDto
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CardDto> Cards { get; set; } = new List<CardDto>();
    }
}