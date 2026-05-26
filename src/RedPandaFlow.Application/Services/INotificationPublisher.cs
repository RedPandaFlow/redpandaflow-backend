using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Services
{
    public interface INotificationPublisher
    {
        Task PublishAsync(Guid userId, NotificationDto notification);
    }
}
