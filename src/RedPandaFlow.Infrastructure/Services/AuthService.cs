using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Interfaces.Services;
using RedPandaFlow.Domain.Entities;
using RedPandaFlow.Infrastructure.Config;
using RedPandaFlow.Infrastructure.Data;

namespace RedPandaFlow.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly RedPandaFlowDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            RedPandaFlowDbContext context,
            IJwtTokenService jwtTokenService,
            JwtSettings jwtSettings,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var email = NormalizeEmail(request.Email);
                var username = request.Username.Trim();

                var exists = await _context.Users
                    .AnyAsync(u => u.Email == email || u.Username == username);

                if (exists)
                {
                    return Fail("User already exists.");
                }

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var refreshToken = await IssueRefreshTokenAsync(user);

                return Success("Registration successful.", user, refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed.");
                return Fail("Registration failed.");
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var email = NormalizeEmail(request.Email);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Fail("Invalid email or password.");
                }

                var refreshToken = await IssueRefreshTokenAsync(user);

                return Success("Login successful.", user, refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed.");
                return Fail("Login failed.");
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var existing = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (existing == null)
            {
                return Fail("Invalid token.");
            }

            if (existing.IsRevoked)
            {
                _logger.LogWarning(
                    "Reuse of revoked refresh token detected for user {UserId}. Revoking all active tokens.",
                    existing.UserId);

                await RevokeAllActiveTokensAsync(existing.UserId, "Token reuse detected.");
                return Fail("Invalid token.");
            }

            if (existing.IsExpired)
            {
                return Fail("Token expired.");
            }

            var newRefreshToken = await RotateRefreshTokenAsync(existing);
            var accessToken = _jwtTokenService.GenerateAccessToken(
                existing.User.Id, existing.User.Username);

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                User = ToDto(existing.User)
            };
        }

        public async Task<bool> LogoutAsync(Guid userId)
        {
            try
            {
                await RevokeAllActiveTokensAsync(userId, "User logout.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed for user {UserId}", userId);
                return false;
            }
        }

        public async Task<AuthResponse> DeleteAccountAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Fail("User not found.");
            }

            var ownedWorkspaces = await _context.Workspaces
                .Where(w => w.OwnerId == userId)
                .Select(w => new { w.Id, OtherMembers = w.Members.Count(m => m.UserId != userId) })
                .ToListAsync();

            if (ownedWorkspaces.Any(w => w.OtherMembers > 0))
            {
                return Fail("Transfer ownership of your workspaces before deleting your account.");
            }

            var ownedWorkspaceIds = ownedWorkspaces.Select(w => w.Id).ToHashSet();

            var blockingBoards = await _context.Boards
                .Where(b => b.OwnerId == userId && !ownedWorkspaceIds.Contains(b.WorkspaceId))
                .AnyAsync(b => b.Members.Any(m => m.UserId != userId));

            if (blockingBoards)
            {
                return Fail("Transfer ownership of your boards before deleting your account.");
            }

            try
            {
                var comments = await _context.Comments
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                foreach (var comment in comments)
                {
                    comment.UserId = null;
                }

                if (ownedWorkspaceIds.Count > 0)
                {
                    var workspacesToRemove = await _context.Workspaces
                        .Where(w => ownedWorkspaceIds.Contains(w.Id))
                        .ToListAsync();
                    _context.Workspaces.RemoveRange(workspacesToRemove);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return new AuthResponse { Success = true, Message = "Account deleted." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account deletion failed for user {UserId}", userId);
                return Fail("Account deletion failed.");
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                _jwtTokenService.GetPrincipalFromExpiredToken(token);
                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        private async Task<RefreshToken> IssueRefreshTokenAsync(User user)
        {
            var token = new RefreshToken
            {
                UserId = user.Id,
                Token = _jwtTokenService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        private async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken current)
        {
            var replacement = new RefreshToken
            {
                UserId = current.UserId,
                Token = _jwtTokenService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            current.RevokedAt = DateTime.UtcNow;
            current.ReplacedByToken = replacement.Token;
            current.ReasonRevoked = "Rotated.";

            _context.RefreshTokens.Add(replacement);
            await _context.SaveChangesAsync();
            return replacement;
        }

        private async Task RevokeAllActiveTokensAsync(Guid userId, string reason)
        {
            var active = await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var t in active)
            {
                t.RevokedAt = now;
                t.ReasonRevoked = reason;
            }

            await _context.SaveChangesAsync();
        }

        private AuthResponse Success(string message, User user, RefreshToken refreshToken)
        {
            return new AuthResponse
            {
                Success = true,
                Message = message,
                AccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username),
                RefreshToken = refreshToken.Token,
                User = ToDto(user)
            };
        }

        private static AuthResponse Fail(string message)
        {
            return new AuthResponse { Success = false, Message = message };
        }

        private static UserDto ToDto(User user) => new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Biography = user.Biography,
            AvatarUrl = user.AvatarUrl
        };

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
