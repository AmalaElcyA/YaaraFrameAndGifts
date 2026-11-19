using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Api;

public interface IYaaraApiClient
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        string? category = null,
        string? badge = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetFeaturedProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiCartResponse> GetCartAsync(Guid? cartId, CancellationToken cancellationToken = default);
    Task<ApiCartResponse> AddCartItemAsync(
        Guid? cartId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiCartResponse> UpdateCartItemAsync(
        Guid? cartId,
        int productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default);
    Task<ApiCartResponse> RemoveCartItemAsync(
        Guid? cartId,
        int productId,
        CancellationToken cancellationToken = default);
    Task<ApiCartResponse> ClearCartAsync(Guid? cartId, CancellationToken cancellationToken = default);
}

public sealed record ApiCartResponse(Guid CartId, CartDto? Cart);

public sealed class YaaraApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
