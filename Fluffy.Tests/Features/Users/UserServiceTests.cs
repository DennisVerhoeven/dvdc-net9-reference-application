using System.Security.Claims;
using Fluffy.Auth;
using Fluffy.Data;
using Fluffy.Data.Entities.User;
using Fluffy.Features.Users;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fluffy.Tests.Features.Users;

public class UserServiceTests
{
    private readonly FluffyDbContext _dbContext;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<FluffyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new FluffyDbContext(options);
        _tokenServiceMock = new Mock<ITokenService>();
        _userService = new UserService(_dbContext, _tokenServiceMock.Object);
    }

    [Fact]
    public async Task GetUserById_ReturnsUser_WhenUserExists()
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
        var result = await _userService.GetUserById(user.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserById_ReturnsNull_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userService.GetUserById(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task IsEmailRegistered_ReturnsTrue_WhenEmailExists()
    {
        // Arrange
        var user = new UserDao
        {
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
        var result = await _userService.IsEmailRegistered(user.Email, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RegisterUser_CreatesNewUser_WhenValidRequestProvided()
    {
        // Arrange
        var request = new Models.RegisterUserRequest
        (
            "new@example.com",
            "password123",
            "New",
            "User"
        );

        // Act
        var result = await _userService.RegisterUser(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
        Assert.NotNull(result.PasswordHash);
        Assert.Equal(AuthenticationMode.Integrated, result.AuthenticationMode);
    }

    [Fact]
    public async Task LoginUser_ReturnsUserAndTokens_WhenCredentialsAreValid()
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

        var request = new Models.LoginUserRequest
        (
            user.Email,
            password
        );

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _userService.LoginUser(request, CancellationToken.None);

        // Assert
        Assert.Equal(user, result.User);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task LoginUser_ThrowsUnauthorizedAccessException_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new Models.LoginUserRequest
        (
            "nonexistent@example.com",
            "wrongpassword"
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _userService.LoginUser(request, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshUserToken_ReturnsNewTokens_WhenRefreshTokenIsValid()
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

        // Act
        var result = await _userService.RefreshUserToken(
            "old-access-token",
            "valid-refresh-token",
            CancellationToken.None);

        // Assert
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshUserToken_ThrowsUnauthorizedAccessException_WhenRefreshTokenIsInvalid()
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
            RefreshToken = "different-refresh-token",
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

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _userService.RefreshUserToken("old-access-token", "invalid-refresh-token", CancellationToken.None));
    }
}