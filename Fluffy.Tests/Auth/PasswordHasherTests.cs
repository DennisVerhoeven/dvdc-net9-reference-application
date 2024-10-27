using Fluffy.Auth;
using Xunit;

namespace Fluffy.Tests.Auth;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ReturnsNonEmptyString()
    {
        // Arrange
        const string password = "TestPassword123!";

        // Act
        string hashedPassword = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
    }

    [Fact]
    public void HashPassword_GeneratesDifferentHashesForSamePassword()
    {
        // Arrange
        const string password = "TestPassword123!";

        // Act
        string hash1 = PasswordHasher.HashPassword(password);
        string hash2 = PasswordHasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        // Arrange
        const string password = "TestPassword123!";
        string hashedPassword = PasswordHasher.HashPassword(password);

        // Act
        bool result = PasswordHasher.VerifyPassword(hashedPassword, password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        // Arrange
        const string password = "TestPassword123!";
        const string wrongPassword = "WrongPassword123!";
        string hashedPassword = PasswordHasher.HashPassword(password);

        // Act
        bool result = PasswordHasher.VerifyPassword(hashedPassword, wrongPassword);

        // Assert
        Assert.False(result);
    }
}