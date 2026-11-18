using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Services;

public interface ICartApiService
{
    Task<CartDto> CreateAsync(CancellationToken cancellationToken);
    Task<Guid> GetOrCreateCartIdAsync(Guid? requestedCartId, CancellationToken cancellationToken);
    Task<CartDto?> GetByIdAsync(Guid cartId, CancellationToken cancellationToken);
    Task<CartApiResult> AddItemAsync(
        Guid cartId,
        int productId,
        int quantity,
        CancellationToken cancellationToken);
    Task<CartApiResult> UpdateItemAsync(
        Guid cartId,
        int productId,
        int quantity,
        CancellationToken cancellationToken);
    Task<CartApiResult> RemoveItemAsync(
        Guid cartId,
        int productId,
        CancellationToken cancellationToken);
    Task<CartApiStatus> ClearAsync(Guid cartId, CancellationToken cancellationToken);
}

public enum CartApiStatus
{
    Success,
    CartNotFound,
    ProductNotFound,
    CartItemNotFound,
    InvalidQuantity
}

public sealed record CartApiResult(CartApiStatus Status, CartDto? Cart);
