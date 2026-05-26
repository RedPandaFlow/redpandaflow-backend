using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Domain.Enums;

namespace RedPandaFlow.Application.Services
{
    public class NotificationPayload
    {
        public string CardTitle { get; set; } = string.Empty;
        public string BoardTitle { get; set; } = string.Empty;
        public string? FromColumnTitle { get; set; }
        public string? ToColumnTitle { get; set; }
    }

    public interface INotificationService
    {
        Task NotifyBoardMembersAsync(Guid workspaceId, Guid boardId, Guid? cardId, Guid actorUserId, NotificationType type, NotificationPayload payload);
        Task<ServiceResult<List<NotificationDto>>> GetForUserAsync(Guid userId, int limit = 20);
        Task<ServiceResult<bool>> MarkReadAsync(Guid userId, Guid notificationId);
        Task<ServiceResult<bool>> MarkAllReadAsync(Guid userId);
        Task<ServiceResult<bool>> DeleteAllAsync(Guid userId);
    }
}
