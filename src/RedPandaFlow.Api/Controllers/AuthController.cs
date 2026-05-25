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
            if (!AllowedAvatarExtensions.TryGetValue(file.ContentType ?? string.Empty, out var extension))
            {
                return BadRequest(new { message = "Format non supporté. Utilisez PNG, JPEG ou WebP." });
            }

            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (read < header.Length || !MatchesMagicBytes(header, file.ContentType!))
            {
                return BadRequest(new { message = "Le contenu du fichier ne correspond pas à une image valide." });
            }
            stream.Position = 0;

            var fileName = $"{userId:N}-{Guid.NewGuid().ToString("N").Substring(0, 8)}{extension}";
            var directory = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(directory);
            var fullPath = Path.Combine(directory, fileName);

            await using (var output = System.IO.File.Create(fullPath))
            {
                await stream.CopyToAsync(output);
            }

            var newUrl = $"/uploads/avatars/{fileName}";
            var result = await _authService.SetAvatarAsync(userId, newUrl);
            if (!result.Success)
            {
                TryDeleteAvatarFile(newUrl);
                return BadRequest(new { message = result.Message });
            }

            TryDeleteAvatarFile(result.Data?.OldAvatarUrl);
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

            var result = await _authService.SetAvatarAsync(userId, null);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            TryDeleteAvatarFile(result.Data?.OldAvatarUrl);
            return NoContent();
        }

        private const long MaxAvatarBytes = 2 * 1024 * 1024;

        private static readonly Dictionary<string, string> AllowedAvatarExtensions = new()
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp"
        };

        private static bool MatchesMagicBytes(byte[] header, string contentType) => contentType switch
        {
            "image/png" => header.Length >= 8
                && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            "image/jpeg" => header.Length >= 3
                && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/webp" => header.Length >= 12
                && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };

        private void TryDeleteAvatarFile(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl) || !avatarUrl.StartsWith("/uploads/avatars/"))
            {
                return;
            }

            var fileName = Path.GetFileName(avatarUrl);
            var path = Path.Combine(_env.WebRootPath, "uploads", "avatars", fileName);
            try
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete avatar file {Path}", path);
            }
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
