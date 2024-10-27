namespace Fluffy.Features.Carts;

public class Models
{
    public record Cart(Guid Id, Guid UserId, List<CartItem> Items)
    {
    }

    public record CartItem(Guid Id, Guid ProductId, int Quantity, decimal Price)
    {
    };

    public record AddToCartParams(Guid ProductId, int Quantity)
    {
    };
}