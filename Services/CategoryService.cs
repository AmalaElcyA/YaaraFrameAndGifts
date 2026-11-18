using Microsoft.EntityFrameworkCore;
using YaaraFramesGifts.Data;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Services;

public sealed class CategoryService(YaaraDbContext dbContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(
                category.Slug,
                category.Name,
                category.Description,
                category.Icon,
                category.AccentColor))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Slug == slug, cancellationToken);

        return category is null ? null : Map(category);
    }

    public async Task<CategoryMutationResult> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var slugExists = await dbContext.Categories
            .AnyAsync(category => category.Slug == request.Slug, cancellationToken);
        if (slugExists)
        {
            return new CategoryMutationResult(CategoryMutationStatus.SlugAlreadyExists, null);
        }

        var category = new Category
        {
            Slug = request.Slug,
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            AccentColor = request.AccentColor
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CategoryMutationResult(CategoryMutationStatus.Success, Map(category));
    }

    public async Task<CategoryMutationResult> UpdateAsync(
        string slug,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (category is null)
        {
            return new CategoryMutationResult(CategoryMutationStatus.NotFound, null);
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.Icon = request.Icon;
        category.AccentColor = request.AccentColor;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CategoryMutationResult(CategoryMutationStatus.Success, Map(category));
    }

    public async Task<CategoryMutationStatus> DeleteAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (category is null)
        {
            return CategoryMutationStatus.NotFound;
        }

        var hasProducts = await dbContext.Products
            .AnyAsync(product => product.CategorySlug == slug, cancellationToken);
        if (hasProducts)
        {
            return CategoryMutationStatus.ProductsExist;
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CategoryMutationStatus.Success;
    }

    private static CategoryDto Map(Category category) => new(
        category.Slug,
        category.Name,
        category.Description,
        category.Icon,
        category.AccentColor);
}
