using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Services;

namespace RedPandaFlow.Api.Controllers
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/columns/{columnId:guid}/cards")]
    [Authorize]
    public class CardsController : ControllerBase
    {
        private readonly ICardService _cardService;

        public CardsController(ICardService cardService)
        {
            _cardService = cardService;
        }

        [HttpGet("/api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/cards")]
        public async Task<IActionResult> GetCardsByBoardId(Guid workspaceId, Guid boardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.GetCardsByBoardIdAsync(workspaceId, boardId, userId);
            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCardsByColumnId(Guid workspaceId, Guid boardId, Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.GetCardsByColumnIdAsync(workspaceId, boardId, columnId, userId);
            return ToActionResult(result);
        }

        [HttpGet("{cardId:guid}")]
        public async Task<IActionResult> GetById(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.GetCardByIdAsync(workspaceId, boardId, columnId, cardId, userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Guid workspaceId, Guid boardId, Guid columnId, [FromBody] CreateCardRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.CreateCardAsync(workspaceId, boardId, columnId, userId, request);
            return ToActionResult(result);
        }

        [HttpPut("{cardId:guid}")]
        public async Task<IActionResult> Update(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, [FromBody] UpdateCardRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.UpdateCardAsync(workspaceId, boardId, columnId, cardId, userId, request);
            return ToActionResult(result);
        }

        [HttpDelete("{cardId:guid}")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.DeleteCardAsync(workspaceId, boardId, columnId, cardId, userId);
            return ToActionResult(result);
        }

        [HttpPatch("{cardId:guid}/order")]
        public async Task<IActionResult> UpdateOrder(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId, [FromBody] UpdateCardOrderRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.UpdateCardOrderAsync(workspaceId, boardId, columnId, cardId, userId, request);
            return ToActionResult(result);
        }

        [HttpGet("/api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/cards/archived")]
        public async Task<IActionResult> GetArchivedCardsByBoard(Guid workspaceId, Guid boardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.GetArchivedCardsByBoardIdAsync(workspaceId, boardId, userId);
            return ToActionResult(result);
        }
        [HttpGet("archived")]
        public async Task<IActionResult> GetArchivedCardsByColumn(Guid workspaceId, Guid boardId, Guid columnId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.GetArchivedCardsByColumnIdAsync(workspaceId, boardId, columnId, userId);
            return ToActionResult(result);
        }

        [HttpPatch("{cardId:guid}/archive")]
        public async Task<IActionResult> Archive(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.ArchiveCardAsync(workspaceId, boardId, columnId, cardId, userId);
            return ToActionResult(result);
        }

        [HttpPatch("{cardId:guid}/restore")]
        public async Task<IActionResult> Restore(Guid workspaceId, Guid boardId, Guid columnId, Guid cardId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _cardService.RestoreCardAsync(workspaceId, boardId, columnId, cardId, userId);
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