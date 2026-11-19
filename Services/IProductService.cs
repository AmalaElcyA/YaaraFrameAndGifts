using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(
        string? category,
        string? badge,
        CancellationToken cancellationToken);
    Task<PagedResultDto<ProductDto>> GetPageAsync(
        string? category,
        string? badge,
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        bool descending,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductDto>> GetFeaturedAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductMutationResult> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);
    Task<ProductMutationResult> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken);
    Task<ProductMutationStatus> DeleteAsync(int id, CancellationToken cancellationToken);
}

public enum ProductMutationStatus
{
    Success,
    ProductNotFound,
    CategoryNotFound,
    CartItemsExist
}

public sealed record ProductMutationResult(ProductMutationStatus Status, ProductDto? Product);
