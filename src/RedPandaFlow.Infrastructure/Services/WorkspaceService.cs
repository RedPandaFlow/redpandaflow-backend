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
            var memberWorkspaces = await _context.WorkspaceUsers
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
                .ToListAsync();

            var memberWorkspaceIds = memberWorkspaces.Select(w => w.Id).ToHashSet();

            var guestWorkspaces = await _context.BoardUser
                .Where(bu => bu.UserId == userId && !memberWorkspaceIds.Contains(bu.Board.WorkspaceId))
                .Select(bu => bu.Board.Workspace)
                .Distinct()
                .Select(w => new WorkspaceDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    OwnerId = w.OwnerId,
                    CreatedAt = w.CreatedAt,
                    CurrentUserRole = null,
                    MemberCount = w.Members.Count
                })
                .ToListAsync();

            var all = memberWorkspaces
                .Concat(guestWorkspaces)
                .OrderBy(w => w.Name)
                .ToList();

            return ServiceResult<List<WorkspaceDto>>.Ok(all);
        }

        public async Task<ServiceResult<WorkspaceDto>> GetByIdAsync(Guid workspaceId, Guid userId)
        {
            var workspace = await _context.Workspaces
                .Include(w => w.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId);

            if (workspace == null)
            {
                return ServiceResult<WorkspaceDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            var membership = workspace.Members.FirstOrDefault(m => m.UserId == userId);
            if (membership != null)
            {
                return ServiceResult<WorkspaceDto>.Ok(ToDto(workspace, membership.Role));
            }

            var hasBoardAccess = await _context.BoardUser
                .AnyAsync(bu => bu.UserId == userId && bu.Board.WorkspaceId == workspaceId);

            if (!hasBoardAccess)
            {
                return ServiceResult<WorkspaceDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            return ServiceResult<WorkspaceDto>.Ok(ToDto(workspace, null));
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
                Role = Role.Admin
            });

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            return ServiceResult<WorkspaceDto>.Ok(ToDto(workspace, Role.Admin), "Workspace created.");
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

            if (membership.Role != Role.Admin)
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

            var boardMemberships = await _context.BoardUser
                .Include(bu => bu.User)
                .Where(bu => bu.Board.WorkspaceId == workspaceId)
                .ToListAsync();

            var boardIdsByUserId = boardMemberships
                .GroupBy(bu => bu.UserId)
                .ToDictionary(g => g.Key, g => g.Select(bu => bu.BoardId).ToList());

            var workspaceMembers = workspace.Members
                .Select(m => ToMemberDto(m, workspace.OwnerId,
                    boardIdsByUserId.TryGetValue(m.UserId, out var ids) ? ids : new List<Guid>()))
                .ToList();

            var workspaceMemberIds = workspace.Members.Select(m => m.UserId).ToHashSet();

            var guests = boardMemberships
                .Where(bu => !workspaceMemberIds.Contains(bu.UserId))
                .GroupBy(bu => bu.UserId)
                .Select(g => new WorkspaceMemberDto
                {
                    UserId = g.Key,
                    Username = g.First().User?.Username ?? string.Empty,
                    Email = g.First().User?.Email ?? string.Empty,
                    AvatarUrl = g.First().User?.AvatarUrl,
                    Role = null,
                    IsOwner = false,
                    BoardIds = g.Select(bu => bu.BoardId).ToList()
                })
                .ToList();

            var all = workspaceMembers
                .Concat(guests)
                .OrderByDescending(m => m.IsOwner)
                .ThenBy(m => m.Username)
                .ToList();

            return ServiceResult<List<WorkspaceMemberDto>>.Ok(all);
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
            if (caller.Role != Role.Admin)
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
            if (caller.Role != Role.Admin)
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
            if (caller.Role != Role.Admin && !isSelf)
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

        private static WorkspaceDto ToDto(Workspace workspace, Role? currentUserRole) => new()
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt,
            CurrentUserRole = currentUserRole,
            MemberCount = workspace.Members.Count
        };

        private static WorkspaceMemberDto ToMemberDto(WorkspaceUser member, Guid ownerId, List<Guid>? boardIds = null) => new()
        {
            UserId = member.UserId,
            Username = member.User?.Username ?? string.Empty,
            Email = member.User?.Email ?? string.Empty,
            AvatarUrl = member.User?.AvatarUrl,
            Role = member.Role,
            IsOwner = member.UserId == ownerId,
            BoardIds = boardIds ?? new List<Guid>()
        };
    }
}
