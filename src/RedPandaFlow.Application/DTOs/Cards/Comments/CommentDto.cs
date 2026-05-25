namespace RedPandaFlow.Application.DTOs
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid CardId { get; set; }
        public Guid? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}