using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Application.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid? ActorUserId { get; set; }
        public string ActorUsername { get; set; } = string.Empty;
        public string? ActorAvatarUrl { get; set; }
        public NotificationType Type { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid BoardId { get; set; }
        public Guid? CardId { get; set; }
        public string CardTitle { get; set; } = string.Empty;
        public string BoardTitle { get; set; } = string.Empty;
        public string? FromColumnTitle { get; set; }
        public string? ToColumnTitle { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
