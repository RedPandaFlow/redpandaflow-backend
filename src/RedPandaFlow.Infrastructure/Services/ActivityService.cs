using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Services;
using RedPandaFlow.Domain.Entities;
using RedPandaFlow.Domain.Enums;
using RedPandaFlow.Infrastructure.Data;

namespace RedPandaFlow.Infrastructure.Services
{
    public class ActivityService : IActivityService
    {
        private readonly RedPandaFlowDbContext _dbContext;
        private readonly ILogger<ActivityService> _logger;

        public ActivityService(RedPandaFlowDbContext dbContext, ILogger<ActivityService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogCardCreatedAsync(Guid cardId, Guid userId, string toColumnTitle)
        {
            await LogAsync(new Activity
            {
                CardId = cardId,
                UserId = userId,
                Type = ActivityType.Created,
                ToColumnTitle = toColumnTitle,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task LogCardMovedAsync(Guid cardId, Guid userId, string fromColumnTitle, string toColumnTitle)
        {
            await LogAsync(new Activity
            {
                CardId = cardId,
                UserId = userId,
                Type = ActivityType.Moved,
                FromColumnTitle = fromColumnTitle,
                ToColumnTitle = toColumnTitle,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<ServiceResult<List<ActivityDto>>> GetByCardIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            var card = await _dbContext.Cards
                .Include(c => c.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == cardId
                                       && c.ColumnId == columnId
                                       && c.Column.BoardId == boardId
                                       && c.Column.Board.WorkspaceId == workspaceId);

            if (card == null) return ServiceResult<List<ActivityDto>>.Fail("Card not found.", ServiceErrorType.NotFound);

            var board = card.Column.Board;
            var hasAccess = board.Members.Any(m => m.UserId == userId)
                            || board.Workspace.Members.Any(m => m.UserId == userId);
            if (!hasAccess) return ServiceResult<List<ActivityDto>>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var activities = await _dbContext.Activities
                .Where(a => a.CardId == cardId)
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ActivityDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Username = a.User != null ? a.User.Username : "Utilisateur supprimé",
                    UserAvatarUrl = a.User != null ? a.User.AvatarUrl : null,
                    Type = a.Type,
                    FromColumnTitle = a.FromColumnTitle,
                    ToColumnTitle = a.ToColumnTitle,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return ServiceResult<List<ActivityDto>>.Ok(activities);
        }

        private async Task LogAsync(Activity activity)
        {
            try
            {
                _dbContext.Activities.Add(activity);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log activity for card {CardId}", activity.CardId);
            }
        }
    }
}
