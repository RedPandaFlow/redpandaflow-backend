using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RedPandaFlow.Infrastructure.Config;
using RedPandaFlow.Infrastructure.Services;

namespace RedPandaFlow.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(new JwtSettings
    {
        SecretKey = "this-is-a-test-secret-key-of-at-least-32-bytes-long!!",
        Issuer = "RedPandaFlow",
        Audience = "RedPandaFlowUsers",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    });

    [Fact]
    public void GenerateAccessToken_EmbedsUserIdAndUsername()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var token = service.GenerateAccessToken(userId, "alice");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("alice", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("RedPandaFlow", jwt.Issuer);
    }

    [Fact]
    public void GenerateRefreshToken_IsRandomAnd32Bytes()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(32, Convert.FromBase64String(first).Length);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsMatchingPrincipal()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var token = service.GenerateAccessToken(userId, "bob");

        var principal = service.GetPrincipalFromExpiredToken(token);

        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}
