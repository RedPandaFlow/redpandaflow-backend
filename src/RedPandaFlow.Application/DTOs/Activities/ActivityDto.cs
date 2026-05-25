using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Application.DTOs
{
    public class ActivityDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public ActivityType Type { get; set; }
        public string? FromColumnTitle { get; set; }
        public string? ToColumnTitle { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
