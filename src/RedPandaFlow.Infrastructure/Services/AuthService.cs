using Microsoft.EntityFrameworkCore;
using RedPandaFlow.Application.DTOs;
using RedPandaFlow.Application.Interfaces.Services;
using RedPandaFlow.Domain.Entities;
using RedPandaFlow.Infrastructure.Data;
using RedPandaFlow.Infrastructure.Services;

namespace RedPandaFlow.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly RedPandaFlowDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthService(RedPandaFlowDbContext context, IJwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email and password are required."
                    };
                }

                if (request.Password != request.ConfirmPassword)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Passwords do not match."
                    };
                }

                var existingUser = _context.Users.FirstOrDefault(u => u.Email == request.Email || u.Username == request.Username);
                if (existingUser != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User already exists."
                    };
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Email);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful.",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email and password are required."
                    };
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };
                }

                var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Email);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new AuthResponse
                {
                  Success = false,
                  Message = "Invalid or expired token"  
                };
            }

            var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Username, user.Email);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
              Success = true,
              AccessToken = newAccessToken,
              RefreshToken = newRefreshToken,
              User = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email}  
            };
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                _jwtTokenService.GetPrincipalFromExpiredToken(token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
