using Fluffy.Data.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluffy.Data.Entities.Cart;

public class CartDao : DataAccessObject
{
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsExpired { get; set; }

    public List<CartItemDao> CartItems { get; set; }
}

public class CartItemDao : DataAccessObject
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    
    public CartDao Cart { get; set; }
}

public class CartConfiguration : IEntityTypeConfiguration<CartDao>
{
    public void Configure(EntityTypeBuilder<CartDao> builder)
    {
        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.StoreId)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.IsExpired)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.HasOne<UserDao>()
            .WithMany()
            .HasForeignKey(c => c.UserId);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItemDao>
{
    public void Configure(EntityTypeBuilder<CartItemDao> builder)
    {
        builder.Property(ci => ci.CartId)
            .IsRequired();

        builder.Property(ci => ci.ProductId)
            .IsRequired();

        builder.Property(ci => ci.Quantity)
            .IsRequired();
        
        builder.HasOne(ci => ci.Cart)
            .WithMany(c => c.CartItems)
            .HasForeignKey(ci => ci.CartId);

        // TODO: Add foreign key to product later
    }
}
