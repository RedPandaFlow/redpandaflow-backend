using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Services
{
    public interface ICardService
    {
        Task<ServiceResult<List<CardDto>>> GetCardsByBoardIdAsync(Guid workspaceId, Guid boardId, Guid userId);
        Task<ServiceResult<List<CardDto>>> GetCardsByColumnIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid userId);
        Task<ServiceResult<CardDto>> GetCardByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
        Task<ServiceResult<CardDto>> CreateCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid userId, CreateCardRequest request);
        Task<ServiceResult<CardDto>> UpdateCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, UpdateCardRequest request);
        Task<ServiceResult<bool>> UpdateCardOrderAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, UpdateCardOrderRequest request);
        Task<ServiceResult<bool>> DeleteCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
        Task<ServiceResult<List<CardDto>>> GetArchivedCardsByBoardIdAsync(Guid workspaceId, Guid boardId, Guid userId);
        Task<ServiceResult<List<CardDto>>> GetArchivedCardsByColumnIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid userId);
        Task<ServiceResult<CardDto>> ArchiveCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
        Task<ServiceResult<CardDto>> RestoreCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
    }
}