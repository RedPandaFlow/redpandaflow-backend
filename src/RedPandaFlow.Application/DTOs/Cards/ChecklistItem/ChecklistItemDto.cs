namespace RedPandaFlow.Application.DTOs
{
    public class ChecklistItemDto
    {
        public Guid Id { get; set; }
        public Guid ChecklistId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
    }
}