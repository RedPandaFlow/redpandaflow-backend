using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.Interfaces.Services;

namespace RedPandaFlow.Api.Controllers
{
    [ApiController]
    [Route("api/search")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var result = await _searchService.SearchAsync(q ?? string.Empty, userId, limit);

            if (result.Success)
            {
                return Ok(result.Data);
            }

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
