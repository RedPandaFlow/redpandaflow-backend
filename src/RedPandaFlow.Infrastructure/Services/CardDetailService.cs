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
    public class CardDetailService : ICardDetailService
    {
        private readonly RedPandaFlowDbContext _dbContext;
        private readonly ILogger<CardDetailService> _logger;

        public CardDetailService(RedPandaFlowDbContext dbContext, ILogger<CardDetailService> logger)
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

        private async Task<Card?> GetCardWithAccessAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            return await _dbContext.Cards
                .Include(c => c.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == cardId
                                       && c.Column.BoardId == boardId
                                       && c.Column.Board.WorkspaceId == workspaceId);
        }


        public async Task<ServiceResult<List<CommentDto>>> GetCardCommentsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<List<CommentDto>>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null) return ServiceResult<List<CommentDto>>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var comments = await _dbContext.Comments
                .Where(c => c.CardId == cardId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => ToCommentDto(c, c.User))
                .ToListAsync();

            return ServiceResult<List<CommentDto>>.Ok(comments);
        }

        public async Task<ServiceResult<CommentDto>> GetCommentByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId, Guid userId)
        {
            var comment = await _dbContext.Comments
                .Include(c => c.User)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (comment == null) return ServiceResult<CommentDto>.Fail("Comment not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(comment.Card.Column.Board, userId) == null) return ServiceResult<CommentDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            return ServiceResult<CommentDto>.Ok(ToCommentDto(comment, comment.User));
        }

        public async Task<ServiceResult<CommentDto>> AddCommentAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, CreateCommentRequest request)
        {
            var content = request.Content.Trim();
            if (content.Length == 0) return ServiceResult<CommentDto>.Fail("Content cannot be empty.", ServiceErrorType.Validation);

            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<CommentDto>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null || EffectiveRole(card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<CommentDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return ServiceResult<CommentDto>.Fail("User not found.", ServiceErrorType.NotFound);

            var comment = new Comment { CardId = cardId, UserId = userId, Content = content, CreatedAt = DateTime.UtcNow };
            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<CommentDto>.Ok(ToCommentDto(comment, user), "Comment added.");
        }

        public async Task<ServiceResult<CommentDto>> UpdateCommentAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId, Guid userId, UpdateCommentRequest request)
        {
            var content = request.Content.Trim();
            if (content.Length == 0) return ServiceResult<CommentDto>.Fail("Content cannot be empty.", ServiceErrorType.Validation);

            var comment = await _dbContext.Comments
                .Include(c => c.User)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (comment == null) return ServiceResult<CommentDto>.Fail("Comment not found.", ServiceErrorType.NotFound);
            if (comment.UserId != userId) return ServiceResult<CommentDto>.Fail("You can only edit your own comments.", ServiceErrorType.Forbidden);

            comment.Content = content;
            await _dbContext.SaveChangesAsync();

            return ServiceResult<CommentDto>.Ok(ToCommentDto(comment, comment.User), "Comment updated.");
        }

        public async Task<ServiceResult<bool>> DeleteCommentAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId, Guid userId)
        {
            var comment = await _dbContext.Comments
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (comment == null) return ServiceResult<bool>.Fail("Comment not found.", ServiceErrorType.NotFound);
            var role = EffectiveRole(comment.Card.Column.Board, userId);
            if (comment.UserId != userId && role != Role.Admin) return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            _dbContext.Comments.Remove(comment);
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Comment deleted.");
        }


        public async Task<ServiceResult<List<UserDto>>> GetCardMembersAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<List<UserDto>>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null) return ServiceResult<List<UserDto>>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var members = await _dbContext.CardUsers
                .Where(cu => cu.CardId == cardId)
                .Include(cu => cu.User)
                .Select(cu => ToUserDto(cu.User!))
                .ToListAsync();

            return ServiceResult<List<UserDto>>.Ok(members);
        }

        public async Task<ServiceResult<bool>> AssignUserToCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid callerId, AssignUserRequest request)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);

            var role = EffectiveRole(card.Column.Board, callerId);
            if (role == null || role == Role.Viewer) return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var targetRole = EffectiveRole(card.Column.Board, request.UserId);
            if (targetRole == null) return ServiceResult<bool>.Fail("This user is not a member of the board.", ServiceErrorType.Forbidden);

            var alreadyAssigned = await _dbContext.CardUsers.AnyAsync(cu => cu.CardId == cardId && cu.UserId == request.UserId);
            if (alreadyAssigned) return ServiceResult<bool>.Fail("User already assigned.", ServiceErrorType.Conflict);

            _dbContext.CardUsers.Add(new CardUser { CardId = cardId, UserId = request.UserId });
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Member assigned.");
        }

        public async Task<ServiceResult<bool>> UnassignUserFromCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid targetUserId, Guid callerId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);

            var role = EffectiveRole(card.Column.Board, callerId);
            if (!((targetUserId == callerId) || role == Role.Admin)) return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var cardUser = await _dbContext.CardUsers.FirstOrDefaultAsync(cu => cu.CardId == cardId && cu.UserId == targetUserId);
            if (cardUser == null) return ServiceResult<bool>.Fail("Assignment not found.", ServiceErrorType.NotFound);

            _dbContext.CardUsers.Remove(cardUser);
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Member unassigned.");
        }


        public async Task<ServiceResult<List<LabelDto>>> GetBoardLabelsAsync(Guid workspaceId, Guid boardId, Guid userId)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .Include(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId && b.WorkspaceId == workspaceId);

            if (board == null) return ServiceResult<List<LabelDto>>.Fail("Board not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(board, userId) == null) return ServiceResult<List<LabelDto>>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var labels = await _dbContext.Labels
                .Where(l => l.BoardId == boardId)
                .Select(l => ToLabelDto(l))
                .ToListAsync();

            return ServiceResult<List<LabelDto>>.Ok(labels);
        }

        public async Task<ServiceResult<LabelDto>> GetBoardLabelByIdAsync(Guid workspaceId, Guid boardId, Guid labelId, Guid userId)
        {
            var label = await _dbContext.Labels
                .Include(l => l.Board).ThenInclude(b => b.Members)
                .Include(l => l.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(l => l.Id == labelId && l.BoardId == boardId && l.Board!.WorkspaceId == workspaceId);

            if (label == null) return ServiceResult<LabelDto>.Fail("Label not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(label.Board!, userId) == null) return ServiceResult<LabelDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            return ServiceResult<LabelDto>.Ok(ToLabelDto(label));
        }

        public async Task<ServiceResult<LabelDto>> CreateBoardLabelAsync(Guid workspaceId, Guid boardId, Guid userId, CreateLabelRequest request)
        {
            var board = await _dbContext.Boards
                .Include(b => b.Members)
                .Include(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId && b.WorkspaceId == workspaceId);

            if (board == null) return ServiceResult<LabelDto>.Fail("Board not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(board, userId) != Role.Admin) return ServiceResult<LabelDto>.Fail("Only an admin can create labels.", ServiceErrorType.Forbidden);

            var label = new Label { BoardId = boardId, Name = request.Name.Trim(), Color = request.Color.Trim() };
            _dbContext.Labels.Add(label);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<LabelDto>.Ok(ToLabelDto(label), "Label created.");
        }

        public async Task<ServiceResult<LabelDto>> UpdateBoardLabelAsync(Guid workspaceId, Guid boardId, Guid labelId, Guid userId, UpdateLabelRequest request)
        {
            var label = await _dbContext.Labels
                .Include(l => l.Board).ThenInclude(b => b.Members)
                .Include(l => l.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(l => l.Id == labelId && l.BoardId == boardId && l.Board!.WorkspaceId == workspaceId);

            if (label == null) return ServiceResult<LabelDto>.Fail("Label not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(label.Board!, userId) != Role.Admin) return ServiceResult<LabelDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            label.Name = request.Name.Trim();
            label.Color = request.Color.Trim();
            await _dbContext.SaveChangesAsync();

            return ServiceResult<LabelDto>.Ok(ToLabelDto(label), "Label updated.");
        }

        public async Task<ServiceResult<bool>> DeleteBoardLabelAsync(Guid workspaceId, Guid boardId, Guid labelId, Guid userId)
        {
            var label = await _dbContext.Labels
                .Include(l => l.Board).ThenInclude(b => b.Members)
                .Include(l => l.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(l => l.Id == labelId && l.BoardId == boardId && l.Board!.WorkspaceId == workspaceId);

            if (label == null) return ServiceResult<bool>.Fail("Label not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(label.Board!, userId) != Role.Admin) return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            _dbContext.Labels.Remove(label);
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Label deleted.");
        }


        public async Task<ServiceResult<List<LabelDto>>> GetCardLabelsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<List<LabelDto>>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null) return ServiceResult<List<LabelDto>>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var labels = await _dbContext.CardLabels
                .Where(cl => cl.CardId == cardId)
                .Include(cl => cl.Label)
                .Select(cl => ToLabelDto(cl.Label!))
                .ToListAsync();

            return ServiceResult<List<LabelDto>>.Ok(labels);
        }

        public async Task<ServiceResult<bool>> AssignLabelToCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, AssignLabelRequest request)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null || EffectiveRole(card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var label = await _dbContext.Labels.FirstOrDefaultAsync(l => l.Id == request.LabelId && l.BoardId == boardId);
            if (label == null) return ServiceResult<bool>.Fail("Label not found.", ServiceErrorType.NotFound);

            if (await _dbContext.CardLabels.AnyAsync(cl => cl.CardId == cardId && cl.LabelId == request.LabelId))
                return ServiceResult<bool>.Fail("Already assigned.", ServiceErrorType.Conflict);

            _dbContext.CardLabels.Add(new CardLabel { CardId = cardId, LabelId = request.LabelId });
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Label assigned.");
        }

        public async Task<ServiceResult<bool>> UnassignLabelFromCardAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid labelId, Guid userId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<bool>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null || EffectiveRole(card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var cardLabel = await _dbContext.CardLabels.FirstOrDefaultAsync(cl => cl.CardId == cardId && cl.LabelId == labelId);
            if (cardLabel == null) return ServiceResult<bool>.Fail("Not assigned.", ServiceErrorType.NotFound);

            _dbContext.CardLabels.Remove(cardLabel);
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Label unassigned.");
        }


        public async Task<ServiceResult<List<ChecklistDto>>> GetCardChecklistsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<List<ChecklistDto>>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null) return ServiceResult<List<ChecklistDto>>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var checklists = await _dbContext.Checklists
                .Where(c => c.CardId == cardId)
                .Include(c => c.Items)
                .Select(c => ToChecklistDto(c))
                .ToListAsync();

            return ServiceResult<List<ChecklistDto>>.Ok(checklists);
        }

        public async Task<ServiceResult<ChecklistDto>> GetChecklistByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId)
        {
            var checklist = await _dbContext.Checklists
                .Include(c => c.Items)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == checklistId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (checklist == null) return ServiceResult<ChecklistDto>.Fail("Checklist not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(checklist.Card!.Column!.Board!, userId) == null) return ServiceResult<ChecklistDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            return ServiceResult<ChecklistDto>.Ok(ToChecklistDto(checklist));
        }

        public async Task<ServiceResult<ChecklistDto>> CreateChecklistAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid userId, CreateChecklistRequest request)
        {
            var card = await GetCardWithAccessAsync(workspaceId, boardId, columnId, cardId);
            if (card == null) return ServiceResult<ChecklistDto>.Fail("Card not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(card.Column.Board, userId) == null || EffectiveRole(card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<ChecklistDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var checklist = new Checklist { CardId = cardId, Title = request.Title.Trim() };
            _dbContext.Checklists.Add(checklist);
            await _dbContext.SaveChangesAsync();

            return ServiceResult<ChecklistDto>.Ok(ToChecklistDto(checklist), "Checklist created.");
        }

        public async Task<ServiceResult<ChecklistDto>> UpdateChecklistAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId, UpdateChecklistRequest request)
        {
            var checklist = await _dbContext.Checklists
                .Include(c => c.Items)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == checklistId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (checklist == null) return ServiceResult<ChecklistDto>.Fail("Checklist not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(checklist.Card!.Column!.Board!, userId) == null || EffectiveRole(checklist.Card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<ChecklistDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            checklist.Title = request.Title.Trim();
            await _dbContext.SaveChangesAsync();
            return ServiceResult<ChecklistDto>.Ok(ToChecklistDto(checklist), "Checklist updated.");
        }

        public async Task<ServiceResult<bool>> DeleteChecklistAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId)
        {
            var checklist = await _dbContext.Checklists
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == checklistId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (checklist == null) return ServiceResult<bool>.Fail("Not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(checklist.Card!.Column!.Board!, userId) == null || EffectiveRole(checklist.Card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            _dbContext.Checklists.Remove(checklist);
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Checklist deleted.");
        }


        public async Task<ServiceResult<List<ChecklistItemDto>>> GetChecklistItemsAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId)
        {
            var checklist = await _dbContext.Checklists
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == checklistId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (checklist == null) return ServiceResult<List<ChecklistItemDto>>.Fail("Checklist not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(checklist.Card!.Column!.Board!, userId) == null) return ServiceResult<List<ChecklistItemDto>>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var items = await _dbContext.ChecklistItems
                .Where(i => i.ChecklistId == checklistId)
                .Select(i => ToChecklistItemDto(i))
                .ToListAsync();

            return ServiceResult<List<ChecklistItemDto>>.Ok(items);
        }

        public async Task<ServiceResult<ChecklistItemDto>> GetChecklistItemByIdAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId, Guid userId)
        {
            var item = await _dbContext.ChecklistItems
                .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.ChecklistId == checklistId && i.Checklist!.CardId == cardId && i.Checklist.Card.Column!.BoardId == boardId && i.Checklist.Card.Column.Board!.WorkspaceId == workspaceId);

            if (item == null) return ServiceResult<ChecklistItemDto>.Fail("Item not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(item.Checklist!.Card!.Column!.Board!, userId) == null) return ServiceResult<ChecklistItemDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            return ServiceResult<ChecklistItemDto>.Ok(ToChecklistItemDto(item));
        }

        public async Task<ServiceResult<ChecklistItemDto>> AddChecklistItemAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid userId, CreateChecklistItemRequest request)
        {
            var checklist = await _dbContext.Checklists
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(c => c.Id == checklistId && c.CardId == cardId && c.Card.Column.BoardId == boardId && c.Card.Column.Board.WorkspaceId == workspaceId);

            if (checklist == null) return ServiceResult<ChecklistItemDto>.Fail("Checklist not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(checklist.Card!.Column!.Board!, userId) == null || EffectiveRole(checklist.Card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<ChecklistItemDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            var item = new ChecklistItem { ChecklistId = checklistId, Content = request.Content.Trim(), IsFinished = false };
            _dbContext.ChecklistItems.Add(item);
            await _dbContext.SaveChangesAsync();
            return ServiceResult<ChecklistItemDto>.Ok(ToChecklistItemDto(item), "Item added.");
        }

        public async Task<ServiceResult<ChecklistItemDto>> UpdateChecklistItemAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId, Guid userId, UpdateChecklistItemRequest request)
        {
            var item = await _dbContext.ChecklistItems
                .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.ChecklistId == checklistId && i.Checklist!.CardId == cardId && i.Checklist.Card.Column!.BoardId == boardId && i.Checklist.Card.Column.Board!.WorkspaceId == workspaceId);

            if (item == null) return ServiceResult<ChecklistItemDto>.Fail("Item not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(item.Checklist!.Card!.Column!.Board!, userId) == null || EffectiveRole(item.Checklist.Card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<ChecklistItemDto>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            if (request.Content != null) item.Content = request.Content.Trim();
            if (request.IsFinished.HasValue) item.IsFinished = request.IsFinished.Value;

            await _dbContext.SaveChangesAsync();
            return ServiceResult<ChecklistItemDto>.Ok(ToChecklistItemDto(item), "Item updated.");
        }

        public async Task<ServiceResult<bool>> DeleteChecklistItemAsync(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId, Guid userId)
        {
            var item = await _dbContext.ChecklistItems
                .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Members)
                .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(card => card.Column).ThenInclude(col => col.Board).ThenInclude(b => b.Workspace).ThenInclude(w => w.Members)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.ChecklistId == checklistId && i.Checklist!.CardId == cardId && i.Checklist.Card.Column!.BoardId == boardId && i.Checklist.Card.Column.Board!.WorkspaceId == workspaceId);

            if (item == null) return ServiceResult<bool>.Fail("Item not found.", ServiceErrorType.NotFound);
            if (EffectiveRole(item.Checklist!.Card!.Column!.Board!, userId) == null || EffectiveRole(item.Checklist.Card.Column.Board, userId) == Role.Viewer)
                return ServiceResult<bool>.Fail("Forbidden.", ServiceErrorType.Forbidden);

            _dbContext.ChecklistItems.Remove(item);
            await _dbContext.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Item deleted.");
        }


        private static UserDto ToUserDto(User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl
        };

        private static CommentDto ToCommentDto(Comment comment, User? user) => new()
        {
            Id = comment.Id,
            CardId = comment.CardId,
            UserId = comment.UserId,
            Username = user?.Username ?? "Utilisateur supprimé",
            UserAvatarUrl = user?.AvatarUrl,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };

        private static LabelDto ToLabelDto(Label label) => new()
        {
            Id = label.Id,
            BoardId = label.BoardId,
            Name = label.Name,
            Color = label.Color
        };

        private static ChecklistDto ToChecklistDto(Checklist checklist) => new()
        {
            Id = checklist.Id,
            CardId = checklist.CardId,
            Title = checklist.Title,
            Items = checklist.Items?.Select(ToChecklistItemDto).ToList() ?? new()
        };

        private static ChecklistItemDto ToChecklistItemDto(ChecklistItem item) => new()
        {
            Id = item.Id,
            ChecklistId = item.ChecklistId,
            Content = item.Content,
            IsFinished = item.IsFinished
        };
    }
}