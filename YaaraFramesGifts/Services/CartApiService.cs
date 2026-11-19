using System.Data;
using Microsoft.EntityFrameworkCore;
using YaaraFramesGifts.Data;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Services;

public sealed class CartApiService(YaaraDbContext dbContext) : ICartApiService
{
    private const decimal FreeShippingThreshold = 999m;
    private const decimal ShippingFee = 99m;
    private const int MaximumQuantity = 99;

    public async Task<CartDto> CreateAsync(CancellationToken cancellationToken)
    {
        var cart = new Cart();
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(cart);
    }

    public async Task<Guid> GetOrCreateCartIdAsync(
        Guid? requestedCartId,
        CancellationToken cancellationToken)
    {
        if (requestedCartId is { } cartId
            && cartId != Guid.Empty
            && await dbContext.Carts.AnyAsync(cart => cart.Id == cartId, cancellationToken))
        {
            return cartId;
        }

        var cart = new Cart();
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cart.Id;
    }

    public async Task<CartDto?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await GetCartQuery()
            .SingleOrDefaultAsync(item => item.Id == cartId, cancellationToken);

        return cart is null ? null : Map(cart);
    }

    public async Task<CartApiResult> AddItemAsync(
        Guid cartId,
        int productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (!IsValidQuantity(quantity))
        {
            return new CartApiResult(CartApiStatus.InvalidQuantity, null);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        var cart = await GetLockedCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return new CartApiResult(CartApiStatus.CartNotFound, null);
        }

        var productExists = await dbContext.Products
            .AnyAsync(product => product.Id == productId, cancellationToken);
        if (!productExists)
        {
            return new CartApiResult(CartApiStatus.ProductNotFound, null);
        }

        var existingItem = await dbContext.CartItems
            .SingleOrDefaultAsync(
                item => item.CartId == cartId && item.ProductId == productId,
                cancellationToken);

        if (existingItem is not null)
        {
            if (!IsValidQuantity(existingItem.Quantity + quantity))
            {
                return new CartApiResult(CartApiStatus.InvalidQuantity, null);
            }

            existingItem.Quantity += quantity;
        }
        else
        {
            dbContext.CartItems.Add(new CartItem
            {
                CartId = cartId,
                ProductId = productId,
                Quantity = quantity
            });
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetUpdatedCartAsync(cartId, cancellationToken);
    }

    public async Task<CartApiResult> UpdateItemAsync(
        Guid cartId,
        int productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (!IsValidQuantity(quantity))
        {
            return new CartApiResult(CartApiStatus.InvalidQuantity, null);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        var cart = await GetLockedCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return new CartApiResult(CartApiStatus.CartNotFound, null);
        }

        var cartItem = await dbContext.CartItems
            .SingleOrDefaultAsync(
                item => item.CartId == cartId && item.ProductId == productId,
                cancellationToken);
        if (cartItem is null)
        {
            return new CartApiResult(CartApiStatus.CartItemNotFound, null);
        }

        cartItem.Quantity = quantity;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetUpdatedCartAsync(cartId, cancellationToken);
    }

    public async Task<CartApiResult> RemoveItemAsync(
        Guid cartId,
        int productId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        var cart = await GetLockedCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return new CartApiResult(CartApiStatus.CartNotFound, null);
        }

        var cartItem = await dbContext.CartItems
            .SingleOrDefaultAsync(
                item => item.CartId == cartId && item.ProductId == productId,
                cancellationToken);
        if (cartItem is null)
        {
            return new CartApiResult(CartApiStatus.CartItemNotFound, null);
        }

        dbContext.CartItems.Remove(cartItem);
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetUpdatedCartAsync(cartId, cancellationToken);
    }

    public async Task<CartApiStatus> ClearAsync(Guid cartId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        var cart = await GetLockedCartAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return CartApiStatus.CartNotFound;
        }

        await dbContext.CartItems
            .Where(item => item.CartId == cartId)
            .ExecuteDeleteAsync(cancellationToken);
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CartApiStatus.Success;
    }

    private IQueryable<Cart> GetCartQuery() => dbContext.Carts
        .AsNoTracking()
        .Include(cart => cart.Items)
        .ThenInclude(item => item.Product)
        .ThenInclude(product => product.Category);

    private Task<Cart?> GetLockedCartAsync(Guid cartId, CancellationToken cancellationToken) =>
        dbContext.Carts
            .FromSqlInterpolated(
                $"SELECT * FROM [Carts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {cartId}")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<CartApiResult> GetUpdatedCartAsync(
        Guid cartId,
        CancellationToken cancellationToken)
    {
        var cart = await GetByIdAsync(cartId, cancellationToken);
        if (cart is null)
        {
            throw new InvalidOperationException("The cart disappeared after the update completed.");
        }

        return new CartApiResult(CartApiStatus.Success, cart);
    }

    private static bool IsValidQuantity(int quantity) => quantity is >= 1 and <= MaximumQuantity;

    private static CartDto Map(Cart cart)
    {
        var items = cart.Items
            .OrderBy(item => item.Id)
            .Select(item => new CartItemDto(
                item.Id,
                item.ProductId,
                item.Product.Name,
                item.Product.Category.Name,
                item.Product.Description,
                item.Product.ImageUrl,
                item.Product.Price,
                item.Quantity,
                item.Product.Price * item.Quantity))
            .ToArray();

        var itemCount = items.Sum(item => item.Quantity);
        var subtotal = items.Sum(item => item.LineTotal);
        var isFreeShippingEligible = subtotal >= FreeShippingThreshold;
        var shippingFee = isFreeShippingEligible ? 0m : ShippingFee;

        return new CartDto(
            cart.Id,
            new DateTimeOffset(DateTime.SpecifyKind(cart.CreatedAtUtc, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(cart.UpdatedAtUtc, DateTimeKind.Utc)),
            items,
            itemCount,
            subtotal,
            shippingFee,
            subtotal + shippingFee,
            isFreeShippingEligible);
    }
}
