using YaaraFramesGifts.DTOs;

namespace YaaraFramesGifts.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<CategoryMutationResult> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);
    Task<CategoryMutationResult> UpdateAsync(
        string slug,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);
    Task<CategoryMutationStatus> DeleteAsync(string slug, CancellationToken cancellationToken);
}

public enum CategoryMutationStatus
{
    Success,
    NotFound,
    SlugAlreadyExists,
    ProductsExist
}

public sealed record CategoryMutationResult(CategoryMutationStatus Status, CategoryDto? Category);
