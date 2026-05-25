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
    public class CardService : ICardService
    {
        private readonly RedPandaFlowDbContext _dbContext;
        private readonly IActivityService _activityService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CardService> _logger;

        public CardService(RedPandaFlowDbContext dbContext, IActivityService activityService, INotificationService notificationService, ILogger<CardService> logger)
        {
            _dbContext = dbContext;
            _activityService = activityService;
            _notificationService = notificationService;
            _logger = logger;
        }

        private static Role? EffectiveRole(Board board, Guid userId)
        {
            var boardRole = board.Members.FirstOrDefault(m => m.UserId == userId)?.Role;
            if (boardRole != null) return boardRole;
            return board.Workspace?.Members.FirstOrDefault(m => m.UserId == userId)?.Role;
        }

        public async Task<ServiceResult<List<CardDto>>> GetCardsByBoardIdAsync(Guid workspaceId, Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .Include(b => b.Workspace).ThenInclude(w => w.Members)
                .Include(b => b.Columns).ThenInclude(c => c.Cards)
                .FirstOrDefaultAsync(b => b.Id == boardId && b.WorkspaceId == workspaceId);

            if (board == null || EffectiveRole(board, userId) == null)
            {
                return ServiceResult<List<CardDto>>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var cards = board.Columns
                .SelectMany(c => c.Cards)
                .Where(c => !c.IsArchived)
                .OrderBy(c => c.Order)
                .Select(c => ToDto(c))
                .ToList();

            return ServiceResult<List<CardDto>>.Ok(cards);
        }

        public async Task<ServiceResult<List<CardDto>>> GetCardsByColumnIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid userId)
        {
            var column = await GetColumnWithAccessAsync(workspaceId, boardId, columnId);

            if (column == null || EffectiveRole(column.Board, userId) == null)
            {
                return ServiceResult<List<CardDto>>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            var cards = column.Cards
                .Where(c => !c.IsArchived)
                .OrderBy(c => c.Order)
                .Select(c => ToDto(c))
                .ToList();

            return ServiceResult<List<CardDto>>.Ok(cards);
        }

        public async Task<ServiceResult<CardDto>> GetCardByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);

            if (card == null || EffectiveRole(card.Column.Board, userId) == null)
            {
                return ServiceResult<CardDto>.Fail("Card not found.", ServiceErrorType.NotFound);
            }

            return ServiceResult<CardDto>.Ok(ToDto(card));
        }

        public async Task<ServiceResult<CardDto>> CreateCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid userId, CreateCardRequest request)
        {
            var column = await GetColumnWithAccessAsync(workspaceId, boardId, columnId);

            if (column == null)
            {
                return ServiceResult<CardDto>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(column.Board, userId);
            if (role == null)
            {
                return ServiceResult<CardDto>.Fail("Column not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<CardDto>.Fail("Viewers cannot create cards.", ServiceErrorType.Forbidden);
            }

            var nextOrder = column.Cards.Any() ? column.Cards.Max(c => c.Order) + 1 : 0;

            var card = new Card
            {
                ColumnId = columnId,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Order = nextOrder,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Cards.Add(card);
            await _dbContext.SaveChangesAsync();

            await _activityService.LogCardCreatedAsync(card.Id, userId, column.Title);
            await _notificationService.NotifyBoardMembersAsync(
                workspaceId, boardId, card.Id, userId, NotificationType.CardCreated,
                new NotificationPayload
                {
                    CardTitle = card.Title,
                    BoardTitle = column.Board.Title,
                    ToColumnTitle = column.Title
                });

            return ServiceResult<CardDto>.Ok(ToDto(card), "Card created.");
        }

        public async Task<ServiceResult<CardDto>> UpdateCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, UpdateCardRequest request)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);

            if (card == null)
            {
                return ServiceResult<CardDto>.Fail("Card not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(card.Column.Board, userId);
            if (role == null)
            {
                return ServiceResult<CardDto>.Fail("Card not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<CardDto>.Fail("Viewers cannot edit cards.", ServiceErrorType.Forbidden);
            }

            card.Title = request.Title.Trim();
            card.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            card.DueDate = request.DueDate;
            card.IsArchived = request.IsArchived;
            await _dbContext.SaveChangesAsync();

            return ServiceResult<CardDto>.Ok(ToDto(card), "Card updated.");
        }

        public async Task<ServiceResult<bool>> UpdateCardOrderAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, UpdateCardOrderRequest request)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);

            if (card == null)
            {
                return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(card.Column.Board, userId);
            if (role == null)
            {
                return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<bool>.Fail("Viewers cannot reorder cards.", ServiceErrorType.Forbidden);
            }

            string? movedFromColumnTitle = null;
            string? movedToColumnTitle = null;

            // Déplacement vers une autre colonne
            if (request.NewColumnId != card.ColumnId)
            {
                var targetColumn = await _dbContext.Columns
                    .Include(c => c.Cards)
                    .FirstOrDefaultAsync(c => c.Id == request.NewColumnId && c.BoardId == boardId);

                if (targetColumn == null)
                {
                    return ServiceResult<bool>.Fail("Target column not found.", ServiceErrorType.NotFound);
                }

                var oldColumn = card.Column;
                movedFromColumnTitle = oldColumn.Title;
                movedToColumnTitle = targetColumn.Title;

                foreach (var c in oldColumn.Cards.Where(c => c.Id != cardId && c.Order > card.Order))
                {
                    c.Order--;
                }

                foreach (var c in targetColumn.Cards.Where(c => c.Order >= request.NewOrder))
                {
                    c.Order++;
                }

                card.ColumnId = request.NewColumnId;
                card.Order = request.NewOrder;
            }
            else
            {
                // Déplacement dans la même colonne
                var oldOrder = card.Order;
                var newOrder = request.NewOrder;

                if (oldOrder == newOrder)
                {
                    return ServiceResult<bool>.Ok(true);
                }

                if (newOrder < oldOrder)
                {
                    foreach (var c in card.Column.Cards.Where(c => c.Id != cardId && c.Order >= newOrder && c.Order < oldOrder))
                    {
                        c.Order++;
                    }
                }
                else
                {
                    foreach (var c in card.Column.Cards.Where(c => c.Id != cardId && c.Order > oldOrder && c.Order <= newOrder))
                    {
                        c.Order--;
                    }
                }

                card.Order = newOrder;
            }

            await _dbContext.SaveChangesAsync();

            if (movedFromColumnTitle != null && movedToColumnTitle != null)
            {
                await _activityService.LogCardMovedAsync(cardId, userId, movedFromColumnTitle, movedToColumnTitle);
                await _notificationService.NotifyBoardMembersAsync(
                    workspaceId, boardId, cardId, userId, NotificationType.CardMoved,
                    new NotificationPayload
                    {
                        CardTitle = card.Title,
                        BoardTitle = card.Column.Board.Title,
                        FromColumnTitle = movedFromColumnTitle,
                        ToColumnTitle = movedToColumnTitle
                    });
            }

            return ServiceResult<bool>.Ok(true, "Card order updated.");
        }

        public async Task<ServiceResult<bool>> DeleteCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);

            if (card == null)
            {
                return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(card.Column.Board, userId);
            if (role == null)
            {
                return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<bool>.Fail("Viewers cannot delete cards.", ServiceErrorType.Forbidden);
            }

            _dbContext.Cards.Remove(card);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "Card deleted.");
        }

        private async Task<Column?> GetColumnWithAccessAsync(Guid workspaceId, Guid boardId, Guid columnId)
        {
            return await _dbContext.Columns
                .Include(c => c.Cards)
                .Include(c => c.Board).ThenInclude(b => b.Members)
                .Include(c => c.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == columnId
                    && c.BoardId == boardId
                    && c.Board.WorkspaceId == workspaceId);
        }

        private async Task<Card?> GetCardWithAccessAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            return await _dbContext.Cards
                .Include(c => c.Column).ThenInclude(col => col.Cards)
                .Include(c => c.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == cardId
                    && c.ColumnId == columnId
                    && c.Column.BoardId == boardId
                    && c.Column.Board.WorkspaceId == workspaceId);
        }

        public async Task<ServiceResult<List<CardDto>>> GetArchivedCardsByColumnIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid userId)
        {
            var column = await GetColumnWithAccessAsync(workspaceId, boardId, columnId);

            if (column == null || EffectiveRole(column.Board, userId) == null)
            {
                return ServiceResult<List<CardDto>>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            var cards = column.Cards
                .Where(c => c.IsArchived)
                .OrderBy(c => c.Title)
                .Select(c => ToDto(c))
                .ToList();

            return ServiceResult<List<CardDto>>.Ok(cards);
        }

        public async Task<ServiceResult<CardDto>> ArchiveCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            return await SetArchivedAsync(workspaceId, boardId, columnId, cardId, userId, true);
        }

        public async Task<ServiceResult<CardDto>> RestoreCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            return await SetArchivedAsync(workspaceId, boardId, columnId, cardId, userId, false);
        }

        private async Task<ServiceResult<CardDto>> SetArchivedAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, bool archived)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);

            if (card == null)
            {
                return ServiceResult<CardDto>.Fail("Card not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(card.Column.Board, userId);
            if (role == null)
            {
                return ServiceResult<CardDto>.Fail("Card not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<CardDto>.Fail("Viewers cannot archive cards.", ServiceErrorType.Forbidden);
            }

            if (card.IsArchived == archived)
            {
                return ServiceResult<CardDto>.Ok(ToDto(card));
            }

            card.IsArchived = archived;
            await _dbContext.SaveChangesAsync();

            return ServiceResult<CardDto>.Ok(ToDto(card), archived ? "Card archived." : "Card restored.");
        }

        public async Task<ServiceResult<List<CardDto>>> GetArchivedCardsByBoardIdAsync(Guid workspaceId, Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .Include(b => b.Workspace).ThenInclude(w => w.Members)
                .Include(b => b.Columns).ThenInclude(c => c.Cards)
                .FirstOrDefaultAsync(b => b.Id == boardId && b.WorkspaceId == workspaceId);

            if (board == null || EffectiveRole(board, userId) == null)
            {
                return ServiceResult<List<CardDto>>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var cards = board.Columns
                .SelectMany(c => c.Cards)
                .Where(c => c.IsArchived)
                .OrderBy(c => c.Title)
                .Select(c => ToDto(c))
                .ToList();

            return ServiceResult<List<CardDto>>.Ok(cards);
        }

        public static CardDto ToDto(Card card) => new()
        {
            Id = card.Id,
            ColumnId = card.ColumnId,
            Title = card.Title,
            Description = card.Description ?? string.Empty,
            DueDate = card.DueDate,
            Order = card.Order,
            IsArchived = card.IsArchived,
            CreatedAt = card.CreatedAt
        };
    }
}