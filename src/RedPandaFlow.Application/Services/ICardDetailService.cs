using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Services
{
    public interface ICardDetailService
    {
        Task<ServiceResult<List<CommentDto>>> GetCardCommentsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
        Task<ServiceResult<CommentDto>> GetCommentByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId, Guid userId);
        Task<ServiceResult<CommentDto>> AddCommentAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, CreateCommentRequest request);
        Task<ServiceResult<CommentDto>> UpdateCommentAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId, Guid userId, UpdateCommentRequest request);
        Task<ServiceResult<bool>> DeleteCommentAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId, Guid userId);

        Task<ServiceResult<List<UserDto>>> GetCardMembersAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
        Task<ServiceResult<bool>> AssignUserToCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid callerId, AssignUserRequest request);
        Task<ServiceResult<bool>> UnassignUserFromCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid targetUserId, Guid callerId);

        Task<ServiceResult<List<LabelDto>>> GetBoardLabelsAsync(Guid workspaceId, Guid boardId, Guid userId);
        Task<ServiceResult<LabelDto>> GetBoardLabelByIdAsync(Guid workspaceId, Guid boardId, Guid labelId, Guid userId);
        Task<ServiceResult<LabelDto>> CreateBoardLabelAsync(Guid workspaceId, Guid boardId, Guid userId, CreateLabelRequest request);
        Task<ServiceResult<LabelDto>> UpdateBoardLabelAsync(Guid workspaceId, Guid boardId, Guid labelId, Guid userId, UpdateLabelRequest request);
        Task<ServiceResult<bool>> DeleteBoardLabelAsync(Guid workspaceId, Guid boardId, Guid labelId, Guid userId);

        Task<ServiceResult<List<LabelDto>>> GetCardLabelsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
        Task<ServiceResult<bool>> AssignLabelToCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, AssignLabelRequest request);
        Task<ServiceResult<bool>> UnassignLabelFromCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid labelId, Guid userId);

        Task<ServiceResult<List<ChecklistDto>>> GetCardChecklistsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId);
        Task<ServiceResult<ChecklistDto>> GetChecklistByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId);
        Task<ServiceResult<ChecklistDto>> CreateChecklistAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, CreateChecklistRequest request);
        Task<ServiceResult<ChecklistDto>> UpdateChecklistAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId, UpdateChecklistRequest request);
        Task<ServiceResult<bool>> DeleteChecklistAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId);

        Task<ServiceResult<List<ChecklistItemDto>>> GetChecklistItemsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId);
        Task<ServiceResult<ChecklistItemDto>> GetChecklistItemByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId, Guid userId);
        Task<ServiceResult<ChecklistItemDto>> AddChecklistItemAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId, CreateChecklistItemRequest request);
        Task<ServiceResult<ChecklistItemDto>> UpdateChecklistItemAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId, Guid userId, UpdateChecklistItemRequest request);
        Task<ServiceResult<bool>> DeleteChecklistItemAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId, Guid userId);
    }
}