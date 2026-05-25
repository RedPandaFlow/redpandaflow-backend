using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RedPandaFlow.Infrastructure.Data;

namespace RedPandaFlow.Api.Hubs
{
    [Authorize]
    public class BoardPresenceHub : Hub
    {
        private readonly IBoardPresenceTracker _tracker;
        private readonly RedPandaFlowDbContext _db;

        public BoardPresenceHub(IBoardPresenceTracker tracker, RedPandaFlowDbContext db)
        {
            _tracker = tracker;
            _db = db;
        }

        public async Task JoinBoard(string boardIdString)
        {
            if (!Guid.TryParse(boardIdString, out var boardId))
            {
                throw new HubException("Invalid board id.");
            }

            var user = await GetPresenceUserAsync();
            if (user == null)
            {
                throw new HubException("Unauthenticated.");
            }

            if (!await HasBoardAccess(boardId, user.UserId))
            {
                throw new HubException("Board not found.");
            }

            var group = GroupName(boardId);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            var snapshot = _tracker.Add(boardId, Context.ConnectionId, user);
            await Clients.Group(group).SendAsync("PresenceUpdate", new
            {
                boardId,
                users = snapshot
            });
        }

        public async Task LeaveBoard(string boardIdString)
        {
            if (!Guid.TryParse(boardIdString, out var boardId))
            {
                return;
            }

            var group = GroupName(boardId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            var snapshot = _tracker.Remove(boardId, Context.ConnectionId);
            await Clients.Group(group).SendAsync("PresenceUpdate", new
            {
                boardId,
                users = snapshot
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var entries = _tracker.RemoveConnection(Context.ConnectionId);
            foreach (var (boardId, snapshot) in entries)
            {
                await Clients.Group(GroupName(boardId)).SendAsync("PresenceUpdate", new
                {
                    boardId,
                    users = snapshot
                });
            }
            await base.OnDisconnectedAsync(exception);
        }

        private async Task<PresenceUser?> GetPresenceUserAsync()
        {
            var idStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = Context.User?.FindFirstValue(ClaimTypes.Name);
            if (!Guid.TryParse(idStr, out var userId) || string.IsNullOrEmpty(username))
            {
                return null;
            }
            var avatarUrl = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.AvatarUrl)
                .FirstOrDefaultAsync();
            return new PresenceUser(userId, username, avatarUrl);
        }

        private async Task<bool> HasBoardAccess(Guid boardId, Guid userId)
        {
            var board = await _db.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);
            if (board == null) return false;
            if (board.Members.Any(m => m.UserId == userId)) return true;
            return await _db.WorkspaceUsers
                .AnyAsync(wu => wu.WorkspaceId == board.WorkspaceId && wu.UserId == userId);
        }

        private static string GroupName(Guid boardId) => $"board-{boardId}";
    }
}
