using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RedPandaFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class ProfileController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.Identity?.Name;
            var email = User.FindFirstValue(ClaimTypes.Email);

            return Ok(new
            {
                Message = "Accès autorisé !",
                UserId = userId,
                Username = username,
                Email = email
            });
        }
    }
}