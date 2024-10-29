using Microsoft.AspNetCore.Mvc;

namespace Fluffy.Features.Carts;

public static class CartEndpoints
{
    public const string BasePath = "cart";

    private static IResult AddItemToCart([FromBody] Models.AddToCartParams addToCartParams, HttpContext context)
    {
        return TypedResults.Ok(new Models.Cart(Guid.NewGuid(), Guid.NewGuid(),
            [new Models.CartItem(Guid.NewGuid(), addToCartParams.ProductId, addToCartParams.Quantity, 9)]));
    }

    private static IResult GetCartItems(HttpContext context)
    {
        var test = new Random().Next(1, 100);
        if (test > 50) return TypedResults.NotFound();

        return TypedResults.Ok(new Models.Cart(Guid.NewGuid(), Guid.NewGuid(),
            [new Models.CartItem(Guid.NewGuid(), Guid.NewGuid(), 5, 9)]));
    }

    public static RouteGroupBuilder MapCartV1Endpoints(this RouteGroupBuilder group)
    {
        group.MapGet("", GetCartItems).WithName("Get CartItems");
        // ... existing code ...
        group.MapPost("", AddItemToCart).WithName("Add Item To Cart");
        // ... existing code ...

        return group;
    }
}