using RedPandaFlow.Application.Common;
using RedPandaFlow.Application.DTOs;

namespace RedPandaFlow.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(Guid userId);
        Task<bool> ValidateTokenAsync(string token);
        Task<AuthResponse> DeleteAccountAsync(Guid userId);
        Task<ServiceResult<AvatarUpdateResult>> SetAvatarAsync(Guid userId, string? newAvatarUrl);
    }
}
