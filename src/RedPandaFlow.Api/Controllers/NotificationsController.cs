using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.Services;

namespace RedPandaFlow.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine([FromQuery] int limit = 20)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _notificationService.GetForUserAsync(userId, limit);
            return ToActionResult(result);
        }

        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _notificationService.MarkReadAsync(userId, id);
            return ToActionResult(result);
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _notificationService.MarkAllReadAsync(userId);
            return ToActionResult(result);
        }

        private bool TryGetUserId(out Guid userId)
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        private IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            if (result.Success) return Ok(result.Data);
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
