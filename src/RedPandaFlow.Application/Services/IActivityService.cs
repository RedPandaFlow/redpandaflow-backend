using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Services
{
    public interface IActivityService
    {
        Task LogCardCreatedAsync(Guid cardId, Guid userId, string toColumnTitle);
        Task LogCardMovedAsync(Guid cardId, Guid userId, string fromColumnTitle, string toColumnTitle);
        Task<ServiceResult<List<ActivityDto>>> GetByCardIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
    }
}
