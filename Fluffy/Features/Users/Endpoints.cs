using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fluffy.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fluffy.Data;
using Fluffy.Data.Entities.User;

namespace Fluffy.Features.Users;

public static class UserEndpoints
{
    public const string BasePath = "user";

    static async Task<IResult> GetUser(
        HttpContext context, 
        [FromRoute] Guid id,
        FluffyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(MapToResponse(user));
    }

    static async Task<IResult> RegisterUser(
        HttpContext context, 
        [FromBody] Models.RegisterUserRequest request,
        FluffyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check if email already exists
        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
            return TypedResults.Conflict("Email already registered");

        var user = new UserDao
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            RegisteredAt = DateTime.UtcNow,
            AuthenticationMode = AuthenticationMode.Integrated
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/v1/user/{user.Id}", MapToResponse(user));
    }

    private static Models.UserResponse MapToResponse(UserDao user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.RegisteredAt,
        user.LastLogin
    );

static async Task<IResult> LoginUser(
    HttpContext context,
    [FromBody] Models.LoginUserRequest request,
    FluffyDbContext dbContext,
    ITokenService tokenService,
    CancellationToken cancellationToken)
{
    var user = await dbContext.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

    if (user == null || !PasswordHasher.VerifyPassword(user.PasswordHash, request.Password))
        return TypedResults.Unauthorized();

    // Generate tokens
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    var accessToken = tokenService.GenerateAccessToken(claims);
    var refreshToken = tokenService.GenerateRefreshToken();

    // Update user
    user.LastLogin = DateTime.UtcNow;
    user.RefreshToken = refreshToken;
    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
    
    await dbContext.SaveChangesAsync(cancellationToken);

    var response = new Models.LoginResponse(
        AccessToken: accessToken,
        RefreshToken: refreshToken,
        User: MapToResponse(user)
    );

    return TypedResults.Ok(response);
}

static async Task<IResult> RefreshToken(
    HttpContext context,
    [FromBody] Models.RefreshTokenRequest request,
    FluffyDbContext dbContext,
    ITokenService tokenService,
    CancellationToken cancellationToken)
{
    try
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        var user = await dbContext.Users.FindAsync(new object?[] { userId }, cancellationToken);

        if (user == null || 
            user.RefreshToken != request.RefreshToken || 
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return TypedResults.Unauthorized();
        }

        var newAccessToken = tokenService.GenerateAccessToken(principal.Claims);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new Models.TokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken
        ));
    }
    catch
    {
        return TypedResults.Unauthorized();
    }
}
    
    public static RouteGroupBuilder MapUserV1Endpoints(this RouteGroupBuilder group)
    {
        group.MapGet("{id}", GetUser).WithName("Get User");
        group.MapPost("", RegisterUser).WithName("Register User");
        group.MapPost("/auth/login", LoginUser).WithName("Login User");
        group.MapPost("/auth/refresh-token", RefreshToken).WithName("Refresh Token");
        
        return group;
    }
}
