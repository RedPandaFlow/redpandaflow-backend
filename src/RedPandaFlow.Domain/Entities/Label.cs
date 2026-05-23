using System;
using System.Collections.Generic;

namespace RedPandaFlow.Domain.Entities
{
    public class Label
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        public Board? Board { get; set; }
        public ICollection<CardLabel> CardLabels { get; set; } = new List<CardLabel>();
    }
}