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
}