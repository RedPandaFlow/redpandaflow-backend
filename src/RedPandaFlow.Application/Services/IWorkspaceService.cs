using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Interfaces.Services
{
    public interface IWorkspaceService
    {
        Task<ServiceResult<List<WorkspaceDto>>> GetUserWorkspacesAsync(Guid userId);
        Task<ServiceResult<WorkspaceDto>> GetByIdAsync(Guid workspaceId, Guid userId);
        Task<ServiceResult<WorkspaceDto>> CreateAsync(CreateWorkspaceRequest request, Guid userId);
        Task<ServiceResult<WorkspaceDto>> UpdateAsync(Guid workspaceId, UpdateWorkspaceRequest request, Guid userId);
        Task<ServiceResult<bool>> DeleteAsync(Guid workspaceId, Guid userId);

        Task<ServiceResult<List<WorkspaceMemberDto>>> GetMembersAsync(Guid workspaceId, Guid userId);
        Task<ServiceResult<WorkspaceMemberDto>> InviteMemberAsync(Guid workspaceId, InviteMemberRequest request, Guid userId);
        Task<ServiceResult<WorkspaceMemberDto>> UpdateMemberRoleAsync(Guid workspaceId, Guid memberUserId, UpdateMemberRoleRequest request, Guid userId);
        Task<ServiceResult<bool>> RemoveMemberAsync(Guid workspaceId, Guid memberUserId, Guid userId);
    }
}
