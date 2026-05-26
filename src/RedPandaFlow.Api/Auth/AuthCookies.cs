using Microsoft.AspNetCore.Http;
using RedPandaFlow.Infrastructure.Config;

namespace RedPandaFlow.Api.Auth
{
    public static class AuthCookies
    {
        public const string AccessTokenCookie = "access_token";
        public const string RefreshTokenCookie = "refresh_token";

        private const string RefreshTokenPath = "/api/auth";

        public static void SetTokens(
            HttpResponse response,
            string accessToken,
            string refreshToken,
            JwtSettings settings,
            IWebHostEnvironment env)
        {
            var secure = !env.IsDevelopment();
            var sameSite = SameSiteMode.Lax;

            response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = sameSite,
                Path = "/",
                MaxAge = TimeSpan.FromMinutes(settings.AccessTokenExpirationMinutes),
            });

            response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = sameSite,
                Path = RefreshTokenPath,
                MaxAge = TimeSpan.FromDays(settings.RefreshTokenExpirationDays),
            });
        }

        public static void Clear(HttpResponse response, IWebHostEnvironment env)
        {
            var secure = !env.IsDevelopment();
            var sameSite = SameSiteMode.Lax;

            response.Cookies.Delete(AccessTokenCookie, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = sameSite,
                Path = "/",
            });
            response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = sameSite,
                Path = RefreshTokenPath,
            });
        }
    }
}
