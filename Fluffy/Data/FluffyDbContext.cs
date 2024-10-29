using Fluffy.Data.Entities.Cart;
using Fluffy.Data.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Fluffy.Data;

public class FluffyDbContext(DbContextOptions<FluffyDbContext> options) : DbContext(options)
{
    public DbSet<UserDao> Users { get; set; }
    public DbSet<CartDao> Carts { get; set; }
    public DbSet<CartDao> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CartConfiguration());
        modelBuilder.ApplyConfiguration(new CartItemConfiguration());
    }
}