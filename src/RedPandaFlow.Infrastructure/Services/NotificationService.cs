using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Services;
using RedPandaFlow.Domain.Entities;
using RedPandaFlow.Domain.Enums;
using RedPandaFlow.Infrastructure.Data;

namespace RedPandaFlow.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly RedPandaFlowDbContext _dbContext;
        private readonly INotificationPublisher _publisher;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(RedPandaFlowDbContext dbContext, INotificationPublisher publisher, ILogger<NotificationService> logger)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task NotifyBoardMembersAsync(Guid workspaceId, Guid boardId, Guid? cardId, Guid actorUserId, NotificationType type, NotificationPayload payload)
        {
            try
            {
                var actor = await _dbContext.Users
                    .Where(u => u.Id == actorUserId)
                    .Select(u => new { u.Id, u.Username, u.AvatarUrl })
                    .FirstOrDefaultAsync();
                if (actor == null) return;

                var recipientIds = await _dbContext.Boards
                    .Where(b => b.Id == boardId)
                    .SelectMany(b => b.Members
                        .Select(m => m.UserId)
                        .Concat(b.Workspace.Members.Select(m => m.UserId)))
                    .Distinct()
                    .Where(id => id != actorUserId)
                    .ToListAsync();

                if (recipientIds.Count == 0) return;

                var now = DateTime.UtcNow;
                var notifications = recipientIds.Select(rid => new Notification
                {
                    UserId = rid,
                    ActorUserId = actorUserId,
                    Type = type,
                    WorkspaceId = workspaceId,
                    BoardId = boardId,
                    CardId = cardId,
                    ActorUsername = actor.Username,
                    CardTitle = payload.CardTitle,
                    BoardTitle = payload.BoardTitle,
                    FromColumnTitle = payload.FromColumnTitle,
                    ToColumnTitle = payload.ToColumnTitle,
                    IsRead = false,
                    CreatedAt = now
                }).ToList();

                _dbContext.Notifications.AddRange(notifications);
                await _dbContext.SaveChangesAsync();

                foreach (var notification in notifications)
                {
                    var dto = ToDto(notification, actor.AvatarUrl);
                    await _publisher.PublishAsync(notification.UserId, dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notifications for board {BoardId}", boardId);
            }
        }

        public async Task<ServiceResult<List<NotificationDto>>> GetForUserAsync(Guid userId, int limit = 20)
        {
            var safeLimit = Math.Clamp(limit, 1, 100);

            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(safeLimit)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    ActorUserId = n.ActorUserId,
                    ActorUsername = n.ActorUsername,
                    ActorAvatarUrl = n.ActorUser != null ? n.ActorUser.AvatarUrl : null,
                    Type = n.Type,
                    WorkspaceId = n.WorkspaceId,
                    BoardId = n.BoardId,
                    CardId = n.CardId,
                    CardTitle = n.CardTitle,
                    BoardTitle = n.BoardTitle,
                    FromColumnTitle = n.FromColumnTitle,
                    ToColumnTitle = n.ToColumnTitle,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return ServiceResult<List<NotificationDto>>.Ok(notifications);
        }

        public async Task<ServiceResult<bool>> MarkReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null)
            {
                return ServiceResult<bool>.Fail("Notification not found.", ServiceErrorType.NotFound);
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> MarkAllReadAsync(Guid userId)
        {
            var unread = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteAllAsync(Guid userId)
        {
            await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .ExecuteDeleteAsync();
            return ServiceResult<bool>.Ok(true);
        }

        private static NotificationDto ToDto(Notification n, string? actorAvatarUrl) => new()
        {
            Id = n.Id,
            ActorUserId = n.ActorUserId,
            ActorUsername = n.ActorUsername,
            ActorAvatarUrl = actorAvatarUrl,
            Type = n.Type,
            WorkspaceId = n.WorkspaceId,
            BoardId = n.BoardId,
            CardId = n.CardId,
            CardTitle = n.CardTitle,
            BoardTitle = n.BoardTitle,
            FromColumnTitle = n.FromColumnTitle,
            ToColumnTitle = n.ToColumnTitle,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        };
    }
}
