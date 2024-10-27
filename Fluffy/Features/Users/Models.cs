namespace Fluffy.Features.Users;

public class Models
{
    public record UserResponse(
        Guid Id, 
        string Email, 
        string FirstName, 
        string LastName, 
        DateTime RegisteredAt,
        DateTime? LastLogin
    );
    
    public record RegisterUserRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName
    );
    
    public record LoginUserRequest(
        string Email,
        string Password
    );
    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        UserResponse User
    );

    public record RefreshTokenRequest(
        string AccessToken,
        string RefreshToken
    );

    public record TokenResponse(
        string AccessToken,
        string RefreshToken
    );
}
