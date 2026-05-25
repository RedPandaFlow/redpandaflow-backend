using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Domain.Entities
{
    public class Activity
    {
        public Guid Id { get; set; }
        public Guid CardId { get; set; }
        public Guid? UserId { get; set; }
        public ActivityType Type { get; set; }
        public string? FromColumnTitle { get; set; }
        public string? ToColumnTitle { get; set; }
        public DateTime CreatedAt { get; set; }

        public Card Card { get; set; } = null!;
        public User? User { get; set; }
    }
}
