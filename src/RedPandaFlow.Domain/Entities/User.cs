namespace RedPandaFlow.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Biography { get; set; }
        public string? AvatarUrl { get; set; }
        public byte[]? AvatarData { get; set; }
        public string? AvatarContentType { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<CardUser> CardUsers { get; set; } = new List<CardUser>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
