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
    [Route("api/workspaces/{workspaceId:guid}/boards")]
    [Authorize]
    public class BoardsController : ControllerBase
    {
        private readonly IBoardService _boardService;
        private readonly IHubContext<BoardPresenceHub> _hub;

        public BoardsController(IBoardService boardService, IHubContext<BoardPresenceHub> hub)
        {
            _boardService = boardService;
            _hub = hub;
        }

        [HttpGet]
        public async Task<IActionResult> GetBoards(Guid workspaceId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.GetBoardsByWorkspaceIdAsync(workspaceId, userId);
            return ToActionResult(result);
        }

        [HttpGet("{boardId:guid}")]
        public async Task<IActionResult> GetById(Guid workspaceId, Guid boardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.GetBoardByIdAsync(boardId, userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateBoardRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.CreateBoardAsync(workspaceId, userId, request);
            return ToActionResult(result);
        }

        [HttpPut("{boardId:guid}")]
        public async Task<IActionResult> Update(Guid workspaceId, Guid boardId, [FromBody] UpdateBoardRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.UpdateBoardAsync(boardId, userId, request);
            if (result.Success && result.Data != null)
            {
                await BroadcastAsync(boardId, "BoardUpdated", new { id = result.Data.Id, title = result.Data.Title });
            }
            return ToActionResult(result);
        }

        [HttpDelete("{boardId:guid}")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid boardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (boardId == Guid.Empty)
                return BadRequest(new { message = "Invalid board ID." });

            var result = await _boardService.DeleteBoardAsync(workspaceId, boardId, userId);
            if (result.Success)
            {
                await BroadcastAsync(boardId, "BoardDeleted", new { id = boardId });
            }
            return ToActionResult(result);
        }

        [HttpGet("{boardId:guid}/members")]
        public async Task<IActionResult> GetMembers(Guid workspaceId, Guid boardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.GetBoardMembersAsync(boardId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{boardId:guid}/members")]
        public async Task<IActionResult> InviteMember(Guid workspaceId, Guid boardId, [FromBody] InviteMemberRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.InviteBoardMemberAsync(boardId, userId, request);
            return ToActionResult(result);
        }

        [HttpPut("{boardId:guid}/members/{memberUserId:guid}")]
        public async Task<IActionResult> UpdateMemberRole(Guid workspaceId, Guid boardId, Guid memberUserId, [FromBody] UpdateMemberRoleRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.UpdateBoardMemberRoleAsync(boardId, memberUserId, userId, request);
            return ToActionResult(result);
        }

        [HttpDelete("{boardId:guid}/members/{memberUserId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid boardId, Guid memberUserId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _boardService.RemoveBoardMemberAsync(boardId, memberUserId, userId);
            return ToActionResult(result);
        }

        private bool TryGetUserId(out Guid userId)
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        private Task BroadcastAsync(Guid boardId, string eventName, object payload)
        {
            var group = $"board-{boardId}";
            var senderConnectionId = Request.Headers["X-Connection-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(senderConnectionId))
            {
                return _hub.Clients.GroupExcept(group, new[] { senderConnectionId }).SendAsync(eventName, payload);
            }
            return _hub.Clients.Group(group).SendAsync(eventName, payload);
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