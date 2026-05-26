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
    [Route("api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/columns")]
    [Authorize]
    public class ColumnsController : ControllerBase
    {
        private readonly IColumnService _columnService;
        private readonly IHubContext<BoardPresenceHub> _hub;

        public ColumnsController(IColumnService columnService, IHubContext<BoardPresenceHub> hub)
        {
            _columnService = columnService;
            _hub = hub;
        }

        [HttpGet]
        public async Task<IActionResult> GetColumns(Guid boardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.GetColumnsByBoardIdAsync(boardId, userId);
            return ToActionResult(result);
        }

        [HttpGet("{columnId:guid}")]
        public async Task<IActionResult> GetById(Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.GetColumnByIdAsync(columnId, userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid boardId, [FromBody] CreateColumnRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.CreateColumnAsync(boardId, userId, request);
            if (result.Success && result.Data != null)
            {
                await BroadcastAsync(boardId, "ColumnCreated", new { column = result.Data });
            }
            return ToActionResult(result);
        }

        [HttpPut("{columnId:guid}")]
        public async Task<IActionResult> Update(Guid boardId, Guid columnId, [FromBody] UpdateColumnRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.UpdateColumnAsync(columnId, userId, request);
            if (result.Success && result.Data != null)
            {
                await BroadcastAsync(boardId, "ColumnUpdated", new { column = result.Data });
            }
            return ToActionResult(result);
        }

        [HttpDelete("{columnId:guid}")]
        public async Task<IActionResult> Delete(Guid boardId, Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.DeleteColumnAsync(columnId, userId);
            if (result.Success)
            {
                await BroadcastAsync(boardId, "ColumnDeleted", new { id = columnId });
            }
            return ToActionResult(result);
        }

        [HttpPatch("{columnId:guid}/order")]
        public async Task<IActionResult> UpdateOrder(Guid boardId, Guid columnId, [FromBody] UpdateColumnOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.UpdateColumnOrderAsync(columnId, userId, request);
            if (result.Success)
            {
                await BroadcastAsync(boardId, "ColumnOrderChanged", new { columnId, newOrder = request.NewOrder });
            }
            return ToActionResult(result);
        }

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchived(Guid boardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.GetArchivedColumnsByBoardIdAsync(boardId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{columnId:guid}/archive")]
        public async Task<IActionResult> Archive(Guid boardId, Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.ArchiveColumnAsync(columnId, userId);
            if (result.Success)
            {
                await BroadcastAsync(boardId, "ColumnArchived", new { id = columnId });
            }
            return ToActionResult(result);
        }

        [HttpPost("{columnId:guid}/restore")]
        public async Task<IActionResult> Restore(Guid boardId, Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.RestoreColumnAsync(columnId, userId);
            if (result.Success && result.Data != null)
            {
                await BroadcastAsync(boardId, "ColumnRestored", new { column = result.Data });
            }
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