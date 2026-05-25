using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RedPandaFlow.Api.Hubs;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Services;

namespace RedPandaFlow.Api.Controllers
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/columns/{columnId:guid}/cards")]
    [Authorize]
    public class CardDetailController : ControllerBase
    {
        private readonly ICardDetailService _cardDetailService;
        private readonly IHubContext<BoardPresenceHub> _hub;

        public CardDetailController(ICardDetailService cardDetailService, IHubContext<BoardPresenceHub> hub)
        {
            _cardDetailService = cardDetailService;
            _hub = hub;
        }


        [HttpGet("{cardId:guid}/comments")]
        public async Task<IActionResult> GetCardComments(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetCardCommentsAsync(workspaceId, boardId, columnId, cardId, userId);
            return ToActionResult(result);
        }

        [HttpGet("{cardId:guid}/comments/{commentId:guid}")]
        public async Task<IActionResult> GetCommentById(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetCommentByIdAsync(workspaceId, boardId, columnId, cardId, commentId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{cardId:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, [FromBody] CreateCommentRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.AddCommentAsync(workspaceId, boardId, columnId, cardId, userId, request);
            return ToActionResult(result);
        }

        [HttpPut("{cardId:guid}/comments/{commentId:guid}")]
        public async Task<IActionResult> UpdateComment(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId, [FromBody] UpdateCommentRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.UpdateCommentAsync(workspaceId, boardId, columnId, cardId, commentId, userId, request);
            return ToActionResult(result);
        }

        [HttpDelete("{cardId:guid}/comments/{commentId:guid}")]
        public async Task<IActionResult> DeleteComment(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid commentId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.DeleteCommentAsync(workspaceId, boardId, columnId, cardId, commentId, userId);
            return ToActionResult(result);
        }


        [HttpGet("{cardId:guid}/users")]
        public async Task<IActionResult> GetCardMembers(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetCardMembersAsync(workspaceId, boardId, columnId, cardId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{cardId:guid}/users")]
        public async Task<IActionResult> AssignUser(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, [FromBody] AssignUserRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var callerId)) return Unauthorized();
            var result = await _cardDetailService.AssignUserToCardAsync(workspaceId, boardId, columnId, cardId, callerId, request);
            return ToActionResult(result);
        }

        [HttpDelete("{cardId:guid}/users/{targetUserId:guid}")]
        public async Task<IActionResult> UnassignUser(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid targetUserId)
        {
            if (!TryGetUserId(out var callerId)) return Unauthorized();
            var result = await _cardDetailService.UnassignUserFromCardAsync(workspaceId, boardId, columnId, cardId, targetUserId, callerId);
            return ToActionResult(result);
        }


        [HttpGet("~/api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/labels")]
        public async Task<IActionResult> GetBoardLabels(Guid workspaceId, Guid boardId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetBoardLabelsAsync(workspaceId, boardId, userId);
            return ToActionResult(result);
        }

        [HttpGet("~/api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/labels/{labelId:guid}")]
        public async Task<IActionResult> GetBoardLabelById(Guid workspaceId, Guid boardId, Guid labelId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetBoardLabelByIdAsync(workspaceId, boardId, labelId, userId);
            return ToActionResult(result);
        }

        [HttpPost("~/api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/labels")]
        public async Task<IActionResult> CreateBoardLabel(Guid workspaceId, Guid boardId, [FromBody] CreateLabelRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.CreateBoardLabelAsync(workspaceId, boardId, userId, request);
            if (result.Success) await BroadcastCardsChangedAsync(boardId);
            return ToActionResult(result);
        }

        [HttpPut("~/api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/labels/{labelId:guid}")]
        public async Task<IActionResult> UpdateBoardLabel(Guid workspaceId, Guid boardId, Guid labelId, [FromBody] UpdateLabelRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.UpdateBoardLabelAsync(workspaceId, boardId, labelId, userId, request);
            if (result.Success) await BroadcastCardsChangedAsync(boardId);
            return ToActionResult(result);
        }

        [HttpDelete("~/api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/labels/{labelId:guid}")]
        public async Task<IActionResult> DeleteBoardLabel(Guid workspaceId, Guid boardId, Guid labelId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.DeleteBoardLabelAsync(workspaceId, boardId, labelId, userId);
            if (result.Success) await BroadcastCardsChangedAsync(boardId);
            return ToActionResult(result);
        }


        [HttpGet("{cardId:guid}/labels")]
        public async Task<IActionResult> GetCardLabels(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetCardLabelsAsync(workspaceId, boardId, columnId, cardId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{cardId:guid}/labels")]
        public async Task<IActionResult> AssignLabel(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, [FromBody] AssignLabelRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.AssignLabelToCardAsync(workspaceId, boardId, columnId, cardId, userId, request);
            if (result.Success) await BroadcastCardsChangedAsync(boardId);
            return ToActionResult(result);
        }

        [HttpDelete("{cardId:guid}/labels/{labelId:guid}")]
        public async Task<IActionResult> UnassignLabel(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid labelId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.UnassignLabelFromCardAsync(workspaceId, boardId, columnId, cardId, labelId, userId);
            if (result.Success) await BroadcastCardsChangedAsync(boardId);
            return ToActionResult(result);
        }


        [HttpGet("{cardId:guid}/checklists")]
        public async Task<IActionResult> GetCardChecklists(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetCardChecklistsAsync(workspaceId, boardId, columnId, cardId, userId);
            return ToActionResult(result);
        }

        [HttpGet("{cardId:guid}/checklists/{checklistId:guid}")]
        public async Task<IActionResult> GetChecklistById(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetChecklistByIdAsync(workspaceId, boardId, columnId, cardId, checklistId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{cardId:guid}/checklists")]
        public async Task<IActionResult> CreateChecklist(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, [FromBody] CreateChecklistRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.CreateChecklistAsync(workspaceId, boardId, columnId, cardId, userId, request);
            return ToActionResult(result);
        }

        [HttpPut("{cardId:guid}/checklists/{checklistId:guid}")]
        public async Task<IActionResult> UpdateChecklist(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, [FromBody] UpdateChecklistRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.UpdateChecklistAsync(workspaceId, boardId, columnId, cardId, checklistId, userId, request);
            return ToActionResult(result);
        }

        [HttpDelete("{cardId:guid}/checklists/{checklistId:guid}")]
        public async Task<IActionResult> DeleteChecklist(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.DeleteChecklistAsync(workspaceId, boardId, columnId, cardId, checklistId, userId);
            return ToActionResult(result);
        }


        [HttpGet("{cardId:guid}/checklists/{checklistId:guid}/items")]
        public async Task<IActionResult> GetChecklistItems(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetChecklistItemsAsync(workspaceId, boardId, columnId, cardId, checklistId, userId);
            return ToActionResult(result);
        }

        [HttpGet("{cardId:guid}/checklists/{checklistId:guid}/items/{itemId:guid}")]
        public async Task<IActionResult> GetChecklistItemById(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.GetChecklistItemByIdAsync(workspaceId, boardId, columnId, cardId, checklistId, itemId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{cardId:guid}/checklists/{checklistId:guid}/items")]
        public async Task<IActionResult> AddChecklistItem(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, [FromBody] CreateChecklistItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.AddChecklistItemAsync(workspaceId, boardId, columnId, cardId, checklistId, userId, request);
            return ToActionResult(result);
        }

        [HttpPut("{cardId:guid}/checklists/{checklistId:guid}/items/{itemId:guid}")]
        public async Task<IActionResult> UpdateChecklistItem(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId, [FromBody] UpdateChecklistItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.UpdateChecklistItemAsync(workspaceId, boardId, columnId, cardId, checklistId, itemId, userId, request);
            return ToActionResult(result);
        }

        [HttpDelete("{cardId:guid}/checklists/{checklistId:guid}/items/{itemId:guid}")]
        public async Task<IActionResult> DeleteChecklistItem(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, Guid checklistId, Guid itemId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardDetailService.DeleteChecklistItemAsync(workspaceId, boardId, columnId, cardId, checklistId, itemId, userId);
            return ToActionResult(result);
        }


        private bool TryGetUserId(out Guid userId)
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        private Task BroadcastCardsChangedAsync(Guid boardId)
        {
            var group = $"board-{boardId}";
            var payload = new { boardId };
            var senderConnectionId = Request.Headers["X-Connection-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(senderConnectionId))
            {
                return _hub.Clients.GroupExcept(group, new[] { senderConnectionId }).SendAsync("CardsChanged", payload);
            }
            return _hub.Clients.Group(group).SendAsync("CardsChanged", payload);
        }

        private IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
                return Ok(result.Data);

            var payload = new { message = result.Message };
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(payload),
                ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, payload),
                ServiceErrorType.Conflict => Conflict(payload),
                _ => BadRequest(payload)
            };
        }
    }
}