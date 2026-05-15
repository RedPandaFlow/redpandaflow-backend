using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Interfaces.Services;
using RedPandaFlow.Domain.Entities;
using RedPandaFlow.Domain.Enums;
using RedPandaFlow.Infrastructure.Data;

namespace RedPandaFlow.Infrastructure.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly RedPandaFlowDbContext _context;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(RedPandaFlowDbContext context, ILogger<WorkspaceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<List<WorkspaceDto>>> GetUserWorkspacesAsync(Guid userId)
        {
            var workspaces = await _context.WorkspaceUsers
                .Where(wu => wu.UserId == userId)
                .Select(wu => new WorkspaceDto
                {
                    Id = wu.Workspace.Id,
                    Name = wu.Workspace.Name,
                    Description = wu.Workspace.Description,
                    OwnerId = wu.Workspace.OwnerId,
                    CreatedAt = wu.Workspace.CreatedAt,
                    CurrentUserRole = wu.Role,
                    MemberCount = wu.Workspace.Members.Count
                })
                .OrderBy(w => w.Name)
                .ToListAsync();

            return ServiceResult<List<WorkspaceDto>>.Ok(workspaces);
        }

        public async Task<ServiceResult<WorkspaceDto>> GetByIdAsync(Guid workspaceId, Guid userId)
        {
            var membership = await _context.WorkspaceUsers
                .Include(wu => wu.Workspace)
                .ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == userId);

            if (membership == null)
            {
                return ServiceResult<WorkspaceDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            return ServiceResult<WorkspaceDto>.Ok(ToDto(membership.Workspace, membership.Role));
        }

        public async Task<ServiceResult<WorkspaceDto>> CreateAsync(CreateWorkspaceRequest request, Guid userId)
        {
            var workspace = new Workspace
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };
            workspace.Members.Add(new WorkspaceUser
            {
                UserId = userId,
                Role = WorkspaceRole.Admin
            });

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            return ServiceResult<WorkspaceDto>.Ok(ToDto(workspace, WorkspaceRole.Admin), "Workspace created.");
        }

        public async Task<ServiceResult<WorkspaceDto>> UpdateAsync(Guid workspaceId, UpdateWorkspaceRequest request, Guid userId)
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null)
            {
                return ServiceResult<WorkspaceDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            var membership = workspace.Members.FirstOrDefault(m => m.UserId == userId);
            if (membership == null)
            {
                return ServiceResult<WorkspaceDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            if (membership.Role != WorkspaceRole.Admin)
            {
                return ServiceResult<WorkspaceDto>.Fail("Only an admin can update this workspace.", ServiceErrorType.Forbidden);
            }

            workspace.Name = request.Name.Trim();
            workspace.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            await _context.SaveChangesAsync();

            return ServiceResult<WorkspaceDto>.Ok(ToDto(workspace, membership.Role), "Workspace updated.");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid workspaceId, Guid userId)
        {
            var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspace == null)
            {
                return ServiceResult<bool>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            if (workspace.OwnerId != userId)
            {
                return ServiceResult<bool>.Fail("Only the owner can delete this workspace.", ServiceErrorType.Forbidden);
            }

            _context.Workspaces.Remove(workspace);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "Workspace deleted.");
        }

        public async Task<ServiceResult<List<WorkspaceMemberDto>>> GetMembersAsync(Guid workspaceId, Guid userId)
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null || workspace.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<List<WorkspaceMemberDto>>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            var members = workspace.Members
                .Select(m => ToMemberDto(m, workspace.OwnerId))
                .OrderByDescending(m => m.IsOwner)
                .ThenBy(m => m.Username)
                .ToList();

            return ServiceResult<List<WorkspaceMemberDto>>.Ok(members);
        }

        public async Task<ServiceResult<WorkspaceMemberDto>> InviteMemberAsync(Guid workspaceId, InviteMemberRequest request, Guid userId)
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null || workspace.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            var caller = workspace.Members.First(m => m.UserId == userId);
            if (caller.Role != WorkspaceRole.Admin)
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("Only an admin can invite members.", ServiceErrorType.Forbidden);
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var invitedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (invitedUser == null)
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("No user found with this email.", ServiceErrorType.NotFound);
            }

            if (workspace.Members.Any(m => m.UserId == invitedUser.Id))
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("User is already a member of this workspace.", ServiceErrorType.Conflict);
            }

            var member = new WorkspaceUser
            {
                WorkspaceId = workspaceId,
                UserId = invitedUser.Id,
                Role = request.Role
            };
            _context.WorkspaceUsers.Add(member);
            await _context.SaveChangesAsync();

            member.User = invitedUser;
            return ServiceResult<WorkspaceMemberDto>.Ok(ToMemberDto(member, workspace.OwnerId), "Member added.");
        }

        public async Task<ServiceResult<WorkspaceMemberDto>> UpdateMemberRoleAsync(Guid workspaceId, Guid memberUserId, UpdateMemberRoleRequest request, Guid userId)
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null || workspace.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            var caller = workspace.Members.First(m => m.UserId == userId);
            if (caller.Role != WorkspaceRole.Admin)
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("Only an admin can change member roles.", ServiceErrorType.Forbidden);
            }

            if (memberUserId == workspace.OwnerId)
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("The owner's role cannot be changed.", ServiceErrorType.Forbidden);
            }

            var target = workspace.Members.FirstOrDefault(m => m.UserId == memberUserId);
            if (target == null)
            {
                return ServiceResult<WorkspaceMemberDto>.Fail("Member not found in this workspace.", ServiceErrorType.NotFound);
            }

            target.Role = request.Role;
            await _context.SaveChangesAsync();

            return ServiceResult<WorkspaceMemberDto>.Ok(ToMemberDto(target, workspace.OwnerId), "Member role updated.");
        }

        public async Task<ServiceResult<bool>> RemoveMemberAsync(Guid workspaceId, Guid memberUserId, Guid userId)
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null || workspace.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<bool>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            if (memberUserId == workspace.OwnerId)
            {
                return ServiceResult<bool>.Fail("The owner cannot be removed from the workspace.", ServiceErrorType.Forbidden);
            }

            var caller = workspace.Members.First(m => m.UserId == userId);
            var isSelf = memberUserId == userId;
            if (caller.Role != WorkspaceRole.Admin && !isSelf)
            {
                return ServiceResult<bool>.Fail("Only an admin can remove members.", ServiceErrorType.Forbidden);
            }

            var target = workspace.Members.FirstOrDefault(m => m.UserId == memberUserId);
            if (target == null)
            {
                return ServiceResult<bool>.Fail("Member not found in this workspace.", ServiceErrorType.NotFound);
            }

            _context.WorkspaceUsers.Remove(target);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, isSelf ? "You left the workspace." : "Member removed.");
        }

        private static WorkspaceDto ToDto(Workspace workspace, WorkspaceRole currentUserRole) => new()
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt,
            CurrentUserRole = currentUserRole,
            MemberCount = workspace.Members.Count
        };

        private static WorkspaceMemberDto ToMemberDto(WorkspaceUser member, Guid ownerId) => new()
        {
            UserId = member.UserId,
            Username = member.User?.Username ?? string.Empty,
            Email = member.User?.Email ?? string.Empty,
            Role = member.Role,
            IsOwner = member.UserId == ownerId
        };
    }
}
