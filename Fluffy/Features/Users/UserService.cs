using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Fluffy.Data;
using Fluffy.Data.Entities.User;
using Microsoft.EntityFrameworkCore;
using Fluffy.Auth;

namespace Fluffy.Features.Users;

public interface IUserService
{
    Task<UserDao?> GetUserById(Guid id, CancellationToken cancellationToken);
    Task<UserDao> RegisterUser(Models.RegisterUserRequest request, CancellationToken cancellationToken);
    Task<(UserDao User, string AccessToken, string RefreshToken)> LoginUser(Models.LoginUserRequest request, CancellationToken cancellationToken);
    Task<(string AccessToken, string RefreshToken)> RefreshUserToken(string accessToken, string refreshToken, CancellationToken cancellationToken);
    Task<bool> IsEmailRegistered(string email, CancellationToken cancellationToken);
}

public class UserService : IUserService
{
    private readonly FluffyDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public UserService(FluffyDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<UserDao?> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> IsEmailRegistered(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<UserDao> RegisterUser(Models.RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var user = new UserDao
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            RegisteredAt = DateTime.UtcNow,
            AuthenticationMode = AuthenticationMode.Integrated
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<(UserDao User, string AccessToken, string RefreshToken)> LoginUser(
        Models.LoginUserRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !PasswordHasher.VerifyPassword(user.PasswordHash, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.LastLogin = DateTime.UtcNow;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (user, accessToken, refreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshUserToken(
        string accessToken, 
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        var user = await _dbContext.Users.FindAsync(new object?[] { userId }, cancellationToken);

        if (user == null || 
            user.RefreshToken != refreshToken || 
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (newAccessToken, newRefreshToken);
    }
}
