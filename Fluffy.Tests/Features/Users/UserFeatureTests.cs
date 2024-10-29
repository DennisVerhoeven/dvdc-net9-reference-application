using System.Security.Claims;
using Fluffy.Auth;
using Fluffy.Data;
using Fluffy.Data.Entities.User;
using Fluffy.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fluffy.Tests.Features.Users;

public class UserFeatureTests
{
    private readonly FluffyDbContext _dbContext;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly IUserService _userService;
    private readonly HttpContext _httpContext;

    public UserFeatureTests()
    {
        var options = new DbContextOptionsBuilder<FluffyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new FluffyDbContext(options);
        _tokenServiceMock = new Mock<ITokenService>();
        _userService = new UserService(_dbContext, _tokenServiceMock.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task GetUser_ReturnsOkWithUser_WhenUserExists()
    {
        // Arrange
        var user = new UserDao
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = PasswordHasher.HashPassword("testpassword"),
            RegisteredAt = DateTime.UtcNow,
            AuthenticationMode = AuthenticationMode.Integrated
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await UserFeature.GetUser(_httpContext, user.Id, _userService, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Result);
        var okResult = Assert.IsType<Ok<Models.UserResponse>>(result.Result);
        Assert.Equal(user.Id, okResult.Value.Id);
        Assert.Equal(user.Email, okResult.Value.Email);
    }

    [Fact]
    public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await UserFeature.GetUser(_httpContext, nonExistentId, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task RegisterUser_ReturnsCreated_WhenValidRequestProvided()
    {
        // Arrange
        var request = new Models.RegisterUserRequest(
            "new@example.com",
            "password123",
            "New",
            "User"
        );

        // Act
        var result = await UserFeature.RegisterUser(_httpContext, request, _userService, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<Created<Models.UserResponse>>(result.Result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(request.Email, createdResult.Value.Email);
        Assert.Equal(request.FirstName, createdResult.Value.FirstName);
        Assert.Equal(request.LastName, createdResult.Value.LastName);
        Assert.StartsWith("/api/v1/user/", createdResult.Location);
    }

    [Fact]
    public async Task RegisterUser_ReturnsConflict_WhenEmailExists()
    {
        // Arrange
        var existingUser = new UserDao
        {
            Email = "existing@example.com",
            FirstName = "Existing",
            LastName = "User",
            PasswordHash = PasswordHasher.HashPassword("password"),
            RegisteredAt = DateTime.UtcNow,
            AuthenticationMode = AuthenticationMode.Integrated
        };

        await _dbContext.Users.AddAsync(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new Models.RegisterUserRequest(
            "existing@example.com",
            "password123",
            "New",
            "User"
        );

        // Act
        var result = await UserFeature.RegisterUser(_httpContext, request, _userService, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<Conflict<string>>(result.Result);
        Assert.Equal("Email already registered", conflictResult.Value);
    }

    [Fact]
    public async Task LoginUser_ReturnsOkWithTokens_WhenCredentialsAreValid()
    {
        // Arrange
        const string password = "password123";
        var user = new UserDao
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = PasswordHasher.HashPassword(password),
            RegisteredAt = DateTime.UtcNow,
            AuthenticationMode = AuthenticationMode.Integrated
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var request = new Models.LoginUserRequest(user.Email, password);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await UserFeature.LoginUser(_httpContext, request, _userService, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<Models.LoginResponse>>(result.Result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("access-token", okResult.Value.AccessToken);
        Assert.Equal("refresh-token", okResult.Value.RefreshToken);
        Assert.Equal(user.Email, okResult.Value.User.Email);
    }

    [Fact]
    public async Task LoginUser_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new Models.LoginUserRequest("nonexistent@example.com", "wrongpassword");

        // Act
        var result = await UserFeature.LoginUser(_httpContext, request, _userService, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsOkWithNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var user = new UserDao
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = PasswordHasher.HashPassword("testpassword"),
            RegisteredAt = DateTime.UtcNow,
            AuthenticationMode = AuthenticationMode.Integrated,
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        }));

        _tokenServiceMock.Setup(x => x.GetPrincipalFromExpiredToken("old-access-token"))
            .Returns(principal);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("new-access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        var request = new Models.RefreshTokenRequest("old-access-token", "valid-refresh-token");

        // Act
        var result = await UserFeature.RefreshToken(_httpContext, request, _userService, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<Models.TokenResponse>>(result.Result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("new-access-token", okResult.Value.AccessToken);
        Assert.Equal("new-refresh-token", okResult.Value.RefreshToken);
    }

[Fact]
public async Task RefreshToken_ReturnsUnauthorized_WhenRefreshTokenIsInvalid()
{
    // Arrange
    var user = new UserDao
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        FirstName = "Test",
        LastName = "User",
        PasswordHash = PasswordHasher.HashPassword("testpassword"),
        RegisteredAt = DateTime.UtcNow,
        AuthenticationMode = AuthenticationMode.Integrated,
        RefreshToken = "valid-refresh-token",
        RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
    };

    await _dbContext.Users.AddAsync(user);
    await _dbContext.SaveChangesAsync();

    var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
    }));

    _tokenServiceMock.Setup(x => x.GetPrincipalFromExpiredToken("old-access-token"))
        .Returns(principal);

    var request = new Models.RefreshTokenRequest("old-access-token", "invalid-refresh-token");

    // Act
    var result = await UserFeature.RefreshToken(_httpContext, request, _userService, CancellationToken.None);

    // Assert
    Assert.IsType<UnauthorizedHttpResult>(result.Result);
}

}
