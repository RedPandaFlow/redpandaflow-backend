using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public ColumnsController(IColumnService columnService)
        {
            _columnService = columnService;
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
            return ToActionResult(result);
        }

        [HttpPut("{columnId:guid}")]
        public async Task<IActionResult> Update(Guid columnId, [FromBody] UpdateColumnRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.UpdateColumnAsync(columnId, userId, request);
            return ToActionResult(result);
        }

        [HttpDelete("{columnId:guid}")]
        public async Task<IActionResult> Delete(Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.DeleteColumnAsync(columnId, userId);
            return ToActionResult(result);
        }

        [HttpPatch("{columnId:guid}/order")]
        public async Task<IActionResult> UpdateOrder(Guid columnId, [FromBody] UpdateColumnOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.UpdateColumnOrderAsync(columnId, userId, request);
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
        public async Task<IActionResult> Archive(Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.ArchiveColumnAsync(columnId, userId);
            return ToActionResult(result);
        }

        [HttpPost("{columnId:guid}/restore")]
        public async Task<IActionResult> Restore(Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _columnService.RestoreColumnAsync(columnId, userId);
            return ToActionResult(result);
        }

        private bool TryGetUserId(out Guid userId)
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
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