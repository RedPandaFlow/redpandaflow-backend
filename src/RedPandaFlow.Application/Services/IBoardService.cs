using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Services
{
    public interface IBoardService
    {
        Task<ServiceResult<List<BoardDto>>> GetBoardsByWorkspaceIdAsync(Guid workspaceId, Guid userId);
        Task<ServiceResult<BoardDto>> GetBoardByIdAsync(Guid boardId, Guid userId);
        Task<ServiceResult<BoardDto>> CreateBoardAsync(Guid workspaceId, Guid userId, CreateBoardRequest request);
        Task<ServiceResult<BoardDto>> UpdateBoardAsync(Guid boardId, Guid userId, UpdateBoardRequest request);
        Task<ServiceResult<bool>> DeleteBoardAsync(Guid workspaceId, Guid boardId, Guid userId);

        Task<ServiceResult<List<BoardMemberDto>>> GetBoardMembersAsync(Guid boardId, Guid userId);
        Task<ServiceResult<BoardMemberDto>> InviteBoardMemberAsync(Guid boardId, Guid userId, InviteMemberRequest request);
        Task<ServiceResult<BoardMemberDto>> UpdateBoardMemberRoleAsync(Guid boardId, Guid memberUserId, Guid userId, UpdateMemberRoleRequest request);
        Task<ServiceResult<bool>> RemoveBoardMemberAsync(Guid boardId, Guid memberUserId, Guid userId);
    }
}