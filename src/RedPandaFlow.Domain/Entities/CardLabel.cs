using System;

namespace RedPandaFlow.Domain.Entities
{
    public class CardLabel
    {
        public Guid LabelId { get; set; }
        public Guid CardId { get; set; }

        public Label Label { get; set; } = null!;
        public Card Card { get; set; } = null!;
    }
}