using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RedPandaFlow.Api.Auth;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Interfaces.Services;
using RedPandaFlow.Infrastructure.Config;
using RedPandaFlow.Infrastructure.Data;

namespace RedPandaFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;
        private readonly IWebHostEnvironment _env;
        private readonly RedPandaFlowDbContext _db;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            JwtSettings jwtSettings,
            IWebHostEnvironment env,
            RedPandaFlowDbContext db,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _jwtSettings = jwtSettings;
            _env = env;
            _db = db;
            _logger = logger;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);
            if (!result.Success) return BadRequest(result);

            IssueCookies(result);
            return Ok(StripTokens(result));
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            if (!result.Success) return Unauthorized(result);

            IssueCookies(result);
            return Ok(StripTokens(result));
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies[AuthCookies.RefreshTokenCookie];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new AuthResponse { Success = false, Message = "Missing refresh token." });
            }

            var result = await _authService.RefreshTokenAsync(refreshToken);
            if (!result.Success)
            {
                AuthCookies.Clear(Response, _env);
                return Unauthorized(result);
            }

            IssueCookies(result);
            return Ok(StripTokens(result));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Unauthorized();

            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Biography = user.Biography,
                AvatarUrl = user.AvatarUrl,
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                await _authService.LogoutAsync(userId);
            }

            AuthCookies.Clear(Response, _env);
            return NoContent();
        }

        [HttpDelete("account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var result = await _authService.DeleteAccountAsync(userId);
            if (!result.Success)
            {
                return Conflict(result);
            }

            AuthCookies.Clear(Response, _env);
            return NoContent();
        }

        private void IssueCookies(AuthResponse result)
        {
            if (string.IsNullOrEmpty(result.AccessToken) || string.IsNullOrEmpty(result.RefreshToken))
            {
                _logger.LogWarning("Auth result reported success but tokens were missing.");
                return;
            }
            AuthCookies.SetTokens(Response, result.AccessToken, result.RefreshToken, _jwtSettings, _env);
        }

        private static AuthResponse StripTokens(AuthResponse result) => new()
        {
            Success = result.Success,
            Message = result.Message,
            User = result.User,
            AccessToken = null,
            RefreshToken = null,
        };
    }
}
