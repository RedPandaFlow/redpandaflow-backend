namespace RedPandaFlow.Application.DTOs
{
    public class ChecklistDto
    {
        public Guid Id { get; set; }
        public Guid CardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<ChecklistItemDto> Items { get; set; } = new();
    }
}