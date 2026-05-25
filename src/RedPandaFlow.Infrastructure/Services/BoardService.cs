using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Services;
using RedPandaFlow.Domain.Entities;
using RedPandaFlow.Domain.Enums;
using RedPandaFlow.Infrastructure.Data;
using RedPandaFlow.Infrastructure.Services;

namespace RedPandaFlow.Infrastructure.Services
{
    public class BoardService : IBoardService
    {
        private readonly RedPandaFlowDbContext _dbContext;
        private readonly ILogger<BoardService> _logger;

        public BoardService(RedPandaFlowDbContext dbContext, ILogger<BoardService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        public async Task<ServiceResult<List<BoardDto>>> GetBoardsByWorkspaceIdAsync(Guid workspaceId, Guid userId)
        {
            var isWorkspaceMember = await _dbContext.WorkspaceUsers
                .AnyAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == userId);

            var boardMemberBoardIds = await _dbContext.BoardUser
                .Where(bu => bu.UserId == userId && bu.Board.WorkspaceId == workspaceId)
                .Select(bu => bu.BoardId)
                .ToListAsync();

            if (!isWorkspaceMember && boardMemberBoardIds.Count == 0)
            {
                return ServiceResult<List<BoardDto>>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            var query = _dbContext.Boards.Where(b => b.WorkspaceId == workspaceId);
            if (!isWorkspaceMember)
            {
                query = query.Where(b => boardMemberBoardIds.Contains(b.Id));
            }

            var boards = await query
                .Include(b => b.Columns.Where(c => !c.IsArchived).OrderBy(c => c.Order))
                .ThenInclude(c => c.Cards)
                .OrderBy(b => b.Title)
                .Select(b => ToDto(b))
                .ToListAsync();

            return ServiceResult<List<BoardDto>>.Ok(boards);
        }
        public async Task<ServiceResult<BoardDto>> GetBoardByIdAsync(Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Columns.OrderBy(c => c.Order))
                .ThenInclude(c => c.Cards)
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                return ServiceResult<BoardDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            // L'utilisateur doit être membre du board ou du workspace parent
            var hasAccess = board.Members.Any(m => m.UserId == userId)
                || await _dbContext.WorkspaceUsers.AnyAsync(wu => wu.WorkspaceId == board.WorkspaceId && wu.UserId == userId);

            if (!hasAccess)
            {
                return ServiceResult<BoardDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            return ServiceResult<BoardDto>.Ok(ToDto(board));
        }
        public async Task<ServiceResult<BoardDto>> CreateBoardAsync(Guid workspaceId, Guid userId, CreateBoardRequest request)
        {
            var membership = await _dbContext.WorkspaceUsers
                .FirstOrDefaultAsync(wu => wu.WorkspaceId == workspaceId && wu.UserId == userId);

            if (membership == null)
            {
                return ServiceResult<BoardDto>.Fail("Workspace not found.", ServiceErrorType.NotFound);
            }

            if (membership.Role != Role.Admin)
            {
                return ServiceResult<BoardDto>.Fail("Only an admin can create boards.", ServiceErrorType.Forbidden);
            }

            var board = new Board
            {
                WorkspaceId = workspaceId,
                Title = request.Title.Trim(),
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };
            board.Members.Add(new BoardUser
            {
                UserId = userId,
                Role = Role.Admin
            });

            _dbContext.Boards.Add(board);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<BoardDto>.Ok(ToDto(board), "Board created.");
        }
        public async Task<ServiceResult<BoardDto>> UpdateBoardAsync(Guid boardId, Guid userId, UpdateBoardRequest request)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Columns.Where(c => !c.IsArchived).OrderBy(c => c.Order))
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                return ServiceResult<BoardDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var membership = board.Members.FirstOrDefault(m => m.UserId == userId);
            if (membership == null)
            {
                return ServiceResult<BoardDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            if (membership.Role != Role.Admin)
            {
                return ServiceResult<BoardDto>.Fail("Only an admin can update this board.", ServiceErrorType.Forbidden);
            }

            board.Title = request.Title.Trim();
            await _dbContext.SaveChangesAsync();

            return ServiceResult<BoardDto>.Ok(ToDto(board), "Board updated.");
        }
        public async Task<ServiceResult<bool>> DeleteBoardAsync(Guid workspaceId, Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                return ServiceResult<bool>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            if (board.WorkspaceId != workspaceId)
            {
                return ServiceResult<bool>.Fail("Board not found in this workspace.", ServiceErrorType.NotFound);
            }

            var membership = board.Members.FirstOrDefault(m => m.UserId == userId);
            if (membership == null)
            {
                return ServiceResult<bool>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            if (membership.Role != Role.Admin)
            {
                return ServiceResult<bool>.Fail("Only an admin can delete this board.", ServiceErrorType.Forbidden);
            }

            _dbContext.Boards.Remove(board);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "Board deleted.");
        }
        public async Task<ServiceResult<List<BoardMemberDto>>> GetBoardMembersAsync(Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null || board.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<List<BoardMemberDto>>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var members = board.Members
                .Select(m => ToMemberDto(m, board.OwnerId))
                .OrderByDescending(m => m.IsOwner)
                .ThenBy(m => m.Username)
                .ToList();

            return ServiceResult<List<BoardMemberDto>>.Ok(members);
        }
        public async Task<ServiceResult<BoardMemberDto>> InviteBoardMemberAsync(Guid boardId, Guid userId, InviteMemberRequest request)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null || board.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<BoardMemberDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var caller = board.Members.First(m => m.UserId == userId);
            if (caller.Role != Role.Admin)
            {
                return ServiceResult<BoardMemberDto>.Fail("Only an admin can invite members.", ServiceErrorType.Forbidden);
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var invitedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (invitedUser == null)
            {
                return ServiceResult<BoardMemberDto>.Fail("No user found with this email.", ServiceErrorType.NotFound);
            }

            if (board.Members.Any(m => m.UserId == invitedUser.Id))
            {
                return ServiceResult<BoardMemberDto>.Fail("User is already a member of this board.", ServiceErrorType.Conflict);
            }

            var member = new BoardUser
            {
                BoardId = boardId,
                UserId = invitedUser.Id,
                Role = request.Role
            };
            _dbContext.BoardUser.Add(member);
            await _dbContext.SaveChangesAsync();

            member.User = invitedUser;
            return ServiceResult<BoardMemberDto>.Ok(ToMemberDto(member, board.OwnerId), "Member added.");
        }
        public async Task<ServiceResult<BoardMemberDto>> UpdateBoardMemberRoleAsync(Guid boardId, Guid memberUserId, Guid userId, UpdateMemberRoleRequest request)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null || board.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<BoardMemberDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var caller = board.Members.First(m => m.UserId == userId);
            if (caller.Role != Role.Admin)
            {
                return ServiceResult<BoardMemberDto>.Fail("Only an admin can change member roles.", ServiceErrorType.Forbidden);
            }

            if (memberUserId == board.OwnerId)
            {
                return ServiceResult<BoardMemberDto>.Fail("The owner's role cannot be changed.", ServiceErrorType.Forbidden);
            }

            var target = board.Members.FirstOrDefault(m => m.UserId == memberUserId);
            if (target == null)
            {
                return ServiceResult<BoardMemberDto>.Fail("Member not found in this board.", ServiceErrorType.NotFound);
            }

            target.Role = request.Role;
            await _dbContext.SaveChangesAsync();

            return ServiceResult<BoardMemberDto>.Ok(ToMemberDto(target, board.OwnerId), "Member role updated.");
        }
        public async Task<ServiceResult<bool>> RemoveBoardMemberAsync(Guid boardId, Guid memberUserId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null || board.Members.All(m => m.UserId != userId))
            {
                return ServiceResult<bool>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            if (memberUserId == board.OwnerId)
            {
                return ServiceResult<bool>.Fail("The owner cannot be removed from the board.", ServiceErrorType.Forbidden);
            }

            var caller = board.Members.First(m => m.UserId == userId);
            var isSelf = memberUserId == userId;
            if (caller.Role != Role.Admin && !isSelf)
            {
                return ServiceResult<bool>.Fail("Only an admin can remove members.", ServiceErrorType.Forbidden);
            }

            var target = board.Members.FirstOrDefault(m => m.UserId == memberUserId);
            if (target == null)
            {
                return ServiceResult<bool>.Fail("Member not found in this board.", ServiceErrorType.NotFound);
            }

            _dbContext.BoardUser.Remove(target);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, isSelf ? "You left the board." : "Member removed.");
        }
        private static BoardDto ToDto(Board board) => new()
        {
            Id = board.Id,
            WorkspaceId = board.WorkspaceId,
            Title = board.Title,
            CreatedAt = board.CreatedAt,
            Columns = board.Columns
                .OrderBy(c => c.Order)
                .Select(c => ColumnService.ToDto(c))
                .ToList()
        };
        private static BoardMemberDto ToMemberDto(BoardUser member, Guid ownerId) => new()
        {
            UserId = member.UserId,
            Username = member.User?.Username ?? string.Empty,
            Email = member.User?.Email ?? string.Empty,
            Role = member.Role,
            IsOwner = member.UserId == ownerId
        };
    }
}