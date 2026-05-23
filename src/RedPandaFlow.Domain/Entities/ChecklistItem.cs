using System;

namespace RedPandaFlow.Domain.Entities
{
    public class ChecklistItem
    {
        public Guid Id { get; set; }
        public Guid ChecklistId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsFinished { get; set; } = false;

        public Checklist Checklist { get; set; } = null!;
    }
}