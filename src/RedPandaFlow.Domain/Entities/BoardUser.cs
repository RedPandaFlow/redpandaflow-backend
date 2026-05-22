using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Domain.Entities
{
    public class BoardUser
    {
        public Guid BoardId { get; set; }
        public Guid UserId { get; set; }
        public Role Role { get; set; }

        public Board Board { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
