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

        [HttpPost("avatar")]
        [Authorize]
        [RequestSizeLimit(MaxAvatarBytes + 4096)]
        public async Task<IActionResult> UploadAvatar([FromForm(Name = "file")] IFormFile? file)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier reçu." });
            }
            if (file.Length > MaxAvatarBytes)
            {
                return BadRequest(new { message = "Le fichier dépasse 2 Mo." });
            }
            if (!AllowedAvatarTypes.Contains(file.ContentType ?? string.Empty))
            {
                return BadRequest(new { message = "Format non supporté. Utilisez PNG, JPEG ou WebP." });
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            if (!MatchesMagicBytes(bytes, file.ContentType!))
            {
                return BadRequest(new { message = "Le contenu du fichier ne correspond pas à une image valide." });
            }

            var version = Guid.NewGuid().ToString("N").Substring(0, 8);
            var newUrl = $"/api/auth/avatar/{userId:N}?v={version}";

            var result = await _authService.SetAvatarAsync(userId, bytes, file.ContentType!, newUrl);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { avatarUrl = newUrl });
        }

        [HttpDelete("avatar")]
        [Authorize]
        public async Task<IActionResult> DeleteAvatar()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var result = await _authService.SetAvatarAsync(userId, null, null, null);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return NoContent();
        }

        [HttpGet("avatar/{userId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvatar(Guid userId)
        {
            var avatar = await _authService.GetAvatarAsync(userId);
            if (avatar == null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "public, max-age=86400";
            return File(avatar.Value.Data, avatar.Value.ContentType);
        }

        private const long MaxAvatarBytes = 2 * 1024 * 1024;

        private static readonly HashSet<string> AllowedAvatarTypes = new()
        {
            "image/png",
            "image/jpeg",
            "image/webp"
        };

        private static bool MatchesMagicBytes(byte[] bytes, string contentType) => contentType switch
        {
            "image/png" => bytes.Length >= 8
                && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
                && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
            "image/jpeg" => bytes.Length >= 3
                && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "image/webp" => bytes.Length >= 12
                && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
            _ => false
        };

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
