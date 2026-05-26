using System;
using System.Collections.Generic;

namespace RedPandaFlow.Domain.Entities
{
    public class Checklist
    {
        public Guid Id { get; set; }
        public Guid CardId { get; set; }
        public string Title { get; set; } = string.Empty;

        public Card Card { get; set; } = null!;
        public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
    }
}