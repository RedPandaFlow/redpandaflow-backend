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

        public List<LabelDto> Labels { get; set; } = new();
        public List<UserDto> AssignedUsers { get; set; } = new();
        public List<CommentDto> Comments { get; set; } = new();
        public List<ChecklistDto> Checklists { get; set; } = new();
    }
}