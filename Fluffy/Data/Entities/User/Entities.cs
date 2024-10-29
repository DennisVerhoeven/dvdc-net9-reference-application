using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluffy.Data.Entities.User;

public class UserDao : DataAccessObject
{
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public AuthenticationMode AuthenticationMode { get; set; }
    public string? ExternalAuthenticationIdentifier { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public Guid? PrimaryBillingAddressId { get; set; }
    public Guid? PrimaryShippingAddressId { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum AuthenticationMode
{
    Integrated,
    External
}

public class UserConfiguration : IEntityTypeConfiguration<UserDao>
{
    public void Configure(EntityTypeBuilder<UserDao> builder)
    {
        builder.Property(u => u.Email)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.FirstName)
            .IsRequired();

        builder.Property(u => u.LastName)
            .IsRequired();

        builder.Property(u => u.AuthenticationMode)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                v => v.ToString(),
                v => (AuthenticationMode)Enum.Parse(typeof(AuthenticationMode), v))
            .HasDefaultValue(AuthenticationMode.Integrated);

        builder.Property(u => u.RegisteredAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}