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
    public class ColumnService : IColumnService
    {
        private readonly RedPandaFlowDbContext _dbContext;
        private readonly ILogger<ColumnService> _logger;

        public ColumnService(RedPandaFlowDbContext dbContext, ILogger<ColumnService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        private static Role? EffectiveRole(Board board, Guid userId)
        {
            var boardRole = board.Members.FirstOrDefault(m => m.UserId == userId)?.Role;
            if (boardRole != null) return boardRole;
            return board.Workspace?.Members.FirstOrDefault(m => m.UserId == userId)?.Role;
        }

        public async Task<ServiceResult<List<ColumnDto>>> GetColumnsByBoardIdAsync(Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .Include(b => b.Workspace).ThenInclude(w => w.Members)
                .Include(b => b.Columns)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null || EffectiveRole(board, userId) == null)
            {
                return ServiceResult<List<ColumnDto>>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var columns = board.Columns
                .Where(c => !c.IsArchived)
                .OrderBy(c => c.Order)
                .Select(c => ToDto(c))
                .ToList();

            return ServiceResult<List<ColumnDto>>.Ok(columns);
        }

        public async Task<ServiceResult<List<ColumnDto>>> GetArchivedColumnsByBoardIdAsync(Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .Include(b => b.Workspace).ThenInclude(w => w.Members)
                .Include(b => b.Columns)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null || EffectiveRole(board, userId) == null)
            {
                return ServiceResult<List<ColumnDto>>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var columns = board.Columns
                .Where(c => c.IsArchived)
                .OrderBy(c => c.Title)
                .Select(c => ToDto(c))
                .ToList();

            return ServiceResult<List<ColumnDto>>.Ok(columns);
        }

        public async Task<ServiceResult<ColumnDto>> GetColumnByIdAsync(Guid columnId, Guid userId)
        {
            var column = await _dbContext.Columns
                .Include(c => c.Board).ThenInclude(b => b.Members)
                .Include(c => c.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null || EffectiveRole(column.Board, userId) == null)
            {
                return ServiceResult<ColumnDto>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            return ServiceResult<ColumnDto>.Ok(ToDto(column));
        }

        public async Task<ServiceResult<ColumnDto>> CreateColumnAsync(Guid boardId, Guid userId, CreateColumnRequest request)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .Include(b => b.Workspace).ThenInclude(w => w.Members)
                .Include(b => b.Columns)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
            {
                return ServiceResult<ColumnDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(board, userId);
            if (role == null)
            {
                return ServiceResult<ColumnDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<ColumnDto>.Fail("Viewers cannot create columns.", ServiceErrorType.Forbidden);
            }

            var nextOrder = board.Columns.Any() ? board.Columns.Max(c => c.Order) + 1 : 0;

            var column = new Column
            {
                BoardId = boardId,
                Title = request.Title.Trim(),
                Order = nextOrder,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Columns.Add(column);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<ColumnDto>.Ok(ToDto(column), "Column created.");
        }

        public async Task<ServiceResult<ColumnDto>> UpdateColumnAsync(Guid columnId, Guid userId, UpdateColumnRequest request)
        {
            var column = await _dbContext.Columns
                .Include(c => c.Board).ThenInclude(b => b.Members)
                .Include(c => c.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
            {
                return ServiceResult<ColumnDto>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(column.Board, userId);
            if (role == null)
            {
                return ServiceResult<ColumnDto>.Fail("Column not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<ColumnDto>.Fail("Viewers cannot edit columns.", ServiceErrorType.Forbidden);
            }

            column.Title = request.Title.Trim();
            column.IsArchived = request.IsArchived;
            await _dbContext.SaveChangesAsync();

            return ServiceResult<ColumnDto>.Ok(ToDto(column), "Column updated.");
        }

        public async Task<ServiceResult<bool>> DeleteColumnAsync(Guid columnId, Guid userId)
        {
            var column = await _dbContext.Columns
                .Include(c => c.Board).ThenInclude(b => b.Members)
                .Include(c => c.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
            {
                return ServiceResult<bool>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(column.Board, userId);
            if (role == null)
            {
                return ServiceResult<bool>.Fail("Column not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<bool>.Fail("Viewers cannot delete columns.", ServiceErrorType.Forbidden);
            }

            _dbContext.Columns.Remove(column);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "Column deleted.");
        }

        public async Task<ServiceResult<bool>> UpdateColumnOrderAsync(Guid columnId, Guid userId, UpdateColumnOrderRequest request)
        {
            var column = await _dbContext.Columns
                .Include(c => c.Board).ThenInclude(b => b.Columns)
                .Include(c => c.Board).ThenInclude(b => b.Members)
                .Include(c => c.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
            {
                return ServiceResult<bool>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(column.Board, userId);
            if (role == null)
            {
                return ServiceResult<bool>.Fail("Column not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<bool>.Fail("Viewers cannot reorder columns.", ServiceErrorType.Forbidden);
            }

            var board = column.Board;
            var oldOrder = column.Order;
            var newOrder = request.NewOrder;

            if (oldOrder == newOrder)
            {
                return ServiceResult<bool>.Ok(true);
            }

            if (newOrder < oldOrder)
            {
                foreach (var col in board.Columns.Where(c => c.Id != columnId && c.Order >= newOrder && c.Order < oldOrder))
                {
                    col.Order++;
                }
            }
            else
            {
                foreach (var col in board.Columns.Where(c => c.Id != columnId && c.Order > oldOrder && c.Order <= newOrder))
                {
                    col.Order--;
                }
            }

            column.Order = newOrder;
            await _dbContext.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true, "Column order updated.");
        }
        public async Task<ServiceResult<ColumnDto>> ArchiveColumnAsync(Guid columnId, Guid userId)
        {
            return await SetArchivedAsync(columnId, userId, true);
        }

        public async Task<ServiceResult<ColumnDto>> RestoreColumnAsync(Guid columnId, Guid userId)
        {
            return await SetArchivedAsync(columnId, userId, false);
        }

        private async Task<ServiceResult<ColumnDto>> SetArchivedAsync(Guid columnId, Guid userId, bool archived)
        {
            var column = await _dbContext.Columns
                .Include(c => c.Cards)
                .Include(c => c.Board).ThenInclude(b => b.Members)
                .Include(c => c.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
            {
                return ServiceResult<ColumnDto>.Fail("Column not found.", ServiceErrorType.NotFound);
            }

            var role = EffectiveRole(column.Board, userId);
            if (role == null)
            {
                return ServiceResult<ColumnDto>.Fail("Column not found.", ServiceErrorType.NotFound);
            }
            if (role == Role.Viewer)
            {
                return ServiceResult<ColumnDto>.Fail("Viewers cannot archive columns.", ServiceErrorType.Forbidden);
            }

            if (column.IsArchived == archived)
            {
                return ServiceResult<ColumnDto>.Ok(ToDto(column));
            }

            column.IsArchived = archived;

            if (column.Cards != null)
            {
                foreach (var card in column.Cards)
                {
                    card.IsArchived = archived;
                }
            }

            await _dbContext.SaveChangesAsync();
            return ServiceResult<ColumnDto>.Ok(ToDto(column), archived ? "Column archived." : "Column restored.");
        }

        public static ColumnDto ToDto(Column column) => new()
        {
            Id = column.Id,
            BoardId = column.BoardId,
            Title = column.Title,
            Order = column.Order,
            IsArchived = column.IsArchived,
            CreatedAt = column.CreatedAt,
            Cards = column.Cards == null ? new List<CardDto>() : column.Cards
                .OrderBy(card => card.Order)
                .Select(card => new CardDto
                {
                    Id = card.Id,
                    ColumnId = card.ColumnId,
                    Title = card.Title,
                    Description = card.Description ?? string.Empty,
                    DueDate = card.DueDate,
                    Order = card.Order,
                    IsArchived = card.IsArchived,
                    CreatedAt = card.CreatedAt
                }).ToList()
        };
    }
}