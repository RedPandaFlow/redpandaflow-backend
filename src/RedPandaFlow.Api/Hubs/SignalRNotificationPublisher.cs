using Microsoft.AspNetCore.SignalR;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Services;

namespace RedPandaFlow.Api.Hubs
{
    public class SignalRNotificationPublisher : INotificationPublisher
    {
        private readonly IHubContext<NotificationsHub> _hub;

        public SignalRNotificationPublisher(IHubContext<NotificationsHub> hub)
        {
            _hub = hub;
        }

        public Task PublishAsync(Guid userId, NotificationDto notification)
        {
            return _hub.Clients.Group(NotificationsHub.GroupName(userId)).SendAsync("NewNotification", notification);
        }
    }
}
