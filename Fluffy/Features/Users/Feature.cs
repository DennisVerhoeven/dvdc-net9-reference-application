using Fluffy.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Fluffy.Features.Users;

public static class UserFeature
{
    public const string BasePath = "user";

    public static IServiceCollection ConfigureUserFeatureV1(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static RouteGroupBuilder MapUserV1Endpoints(this RouteGroupBuilder group)
    {
        group.MapGet("{id}", GetUser).WithName("Get User");
        group.MapPost("", RegisterUser).WithName("Register User");
        group.MapPost("/auth/login",
            LoginUser).WithName("Login User");
        group.MapPost("/auth/refresh-token", RefreshToken).WithName("Refresh Token for User");

        return group;
    }

    public static async Task<Results<Ok<Models.UserResponse>, NotFound>> GetUser(
        HttpContext context,
        [FromRoute] Guid id,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetUserById(id, cancellationToken);

        if (user == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(Models.UserResponse.MapToResponse(user));
    }

    public static async Task<Results<Created<Models.UserResponse>, Conflict<string>>> RegisterUser(
        HttpContext context,
        [FromBody] Models.RegisterUserRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var emailExists = await userService.IsEmailRegistered(request.Email, cancellationToken);

        if (emailExists)
            return TypedResults.Conflict("Email already registered");

        var user = await userService.RegisterUser(request, cancellationToken);

        return TypedResults.Created($"/api/v1/user/{user.Id}", Models.UserResponse.MapToResponse(user));
    }

    public static async Task<Results<Ok<Models.LoginResponse>, UnauthorizedHttpResult>> LoginUser(
        HttpContext context,
        [FromBody] Models.LoginUserRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var (user, accessToken, refreshToken) = await userService.LoginUser(request, cancellationToken);

            var response = new Models.LoginResponse(
                accessToken,
                refreshToken,
                Models.UserResponse.MapToResponse(user)
            );

            return TypedResults.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
    }

    public static async Task<Results<Ok<Models.TokenResponse>, UnauthorizedHttpResult>> RefreshToken(
        HttpContext context,
        [FromBody] Models.RefreshTokenRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        try
        {
            var (newAccessToken, newRefreshToken) = await userService.RefreshUserToken(
                request.AccessToken,
                request.RefreshToken,
                cancellationToken);

            return TypedResults.Ok(new Models.TokenResponse(
                newAccessToken,
                newRefreshToken
            ));
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
    }
}