using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using YaaraFramesGifts.Data;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Models;

namespace YaaraFramesGifts.Services;

public sealed class ProductService(YaaraDbContext dbContext) : IProductService
{
    private static readonly Expression<Func<Product, ProductDto>> ProductProjection = product => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.ImageUrl,
        product.CategorySlug,
        product.Badge,
        product.IsPersonalized,
        new CategoryDto(
            product.Category.Slug,
            product.Category.Name,
            product.Category.Description,
            product.Category.Icon,
            product.Category.AccentColor));

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        string? category,
        string? badge,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(product => product.CategorySlug == category);
        }

        if (!string.IsNullOrWhiteSpace(badge))
        {
            query = query.Where(product => product.Badge == badge);
        }

        return await query
            .OrderBy(product => product.Id)
            .Select(ProductProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetFeaturedAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Badge != null)
            .OrderBy(product => product.Id)
            .Take(4)
            .Select(ProductProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(ProductProjection)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductMutationResult> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Slug == request.CategorySlug, cancellationToken);
        if (category is null)
        {
            return new ProductMutationResult(ProductMutationStatus.CategoryNotFound, null);
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            CategorySlug = category.Slug,
            Category = category,
            Badge = request.Badge,
            IsPersonalized = request.IsPersonalized
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProductMutationResult(ProductMutationStatus.Success, Map(product));
    }

    public async Task<ProductMutationResult> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return new ProductMutationResult(ProductMutationStatus.ProductNotFound, null);
        }

        if (!string.Equals(product.CategorySlug, request.CategorySlug, StringComparison.Ordinal))
        {
            var category = await dbContext.Categories
                .SingleOrDefaultAsync(item => item.Slug == request.CategorySlug, cancellationToken);
            if (category is null)
            {
                return new ProductMutationResult(ProductMutationStatus.CategoryNotFound, null);
            }

            product.Category = category;
            product.CategorySlug = category.Slug;
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.Badge = request.Badge;
        product.IsPersonalized = request.IsPersonalized;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProductMutationResult(ProductMutationStatus.Success, Map(product));
    }

    public async Task<ProductMutationStatus> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return ProductMutationStatus.ProductNotFound;
        }

        var isInCart = await dbContext.CartItems
            .AnyAsync(item => item.ProductId == id, cancellationToken);
        if (isInCart)
        {
            return ProductMutationStatus.CartItemsExist;
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ProductMutationStatus.Success;
    }

    private static ProductDto Map(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.ImageUrl,
        product.CategorySlug,
        product.Badge,
        product.IsPersonalized,
        new CategoryDto(
            product.Category.Slug,
            product.Category.Name,
            product.Category.Description,
            product.Category.Icon,
            product.Category.AccentColor));
}
