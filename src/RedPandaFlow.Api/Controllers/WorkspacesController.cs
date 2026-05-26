using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Interfaces.Services;

namespace RedPandaFlow.Api.Controllers
{
    [ApiController]
    [Route("api/workspaces")]
    [Authorize]
    public class WorkspacesController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspacesController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyWorkspaces()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.GetUserWorkspacesAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.GetByIdAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.CreateAsync(request, userId);
            return ToActionResult(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.UpdateAsync(id, request, userId);
            return ToActionResult(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.DeleteAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpGet("{id:guid}/members")]
        public async Task<IActionResult> GetMembers(Guid id)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.GetMembersAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpPost("{id:guid}/members")]
        public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.InviteMemberAsync(id, request, userId);
            return ToActionResult(result);
        }

        [HttpPut("{id:guid}/members/{memberUserId:guid}")]
        public async Task<IActionResult> UpdateMemberRole(Guid id, Guid memberUserId, [FromBody] UpdateMemberRoleRequest request)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.UpdateMemberRoleAsync(id, memberUserId, request, userId);
            return ToActionResult(result);
        }

        [HttpDelete("{id:guid}/members/{memberUserId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid memberUserId)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var result = await _workspaceService.RemoveMemberAsync(id, memberUserId, userId);
            return ToActionResult(result);
        }

        private bool TryGetUserId(out Guid userId)
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        private IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
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
