using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Domain.Entities
{
    public class CardUser
    {
        public Guid CardId { get; set; }
        public Guid UserId { get; set; }

        public Card Card { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
