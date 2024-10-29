using System.Security.Claims;
using Fluffy.Auth;

namespace Fluffy.Tests.Auth;

public class TokenServiceTests
{
    private readonly AppSettings _appSettings;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _appSettings = new AppSettings
        {
            TokenGeneration = new TokenGeneration
            {
                SecretKey = "vXVH1oR6VSHHqR4QqVLPZzXz2vXVH1oR6VSHHqR4QqVLPZzXzvXVH1oR6VSHHqR4QqVLPZzXz",
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        };
        _tokenService = new TokenService(_appSettings);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidToken()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "user")
        };

        // Act
        var token = _tokenService.GenerateAccessToken(claims);

        // Assert
        Assert.NotEmpty(token);
        Assert.True(_tokenService.ValidateToken(token, out var principal));
        Assert.NotNull(principal);
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotEmpty(refreshToken);
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Act
        var refreshToken1 = _tokenService.GenerateRefreshToken();
        var refreshToken2 = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(refreshToken1, refreshToken2);
    }

    [Fact]
    public void ValidateToken_ReturnsTrueForValidToken()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var token = _tokenService.GenerateAccessToken(claims);

        // Act
        var isValid = _tokenService.ValidateToken(token, out var principal);

        // Assert
        Assert.True(isValid);
        Assert.NotNull(principal);
    }

    [Fact]
    public void ValidateToken_ReturnsFalseForInvalidToken()
    {
        // Arrange
        const string invalidToken = "invalid.token.string";

        // Act
        var isValid = _tokenService.ValidateToken(invalidToken, out var principal);

        // Assert
        Assert.False(isValid);
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsValidPrincipal()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var token = _tokenService.GenerateAccessToken(claims);

        // Act
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ThrowsExceptionForInvalidToken()
    {
        // Arrange
        const string invalidToken = "invalid.token.string";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _tokenService.GetPrincipalFromExpiredToken(invalidToken));
    }
}