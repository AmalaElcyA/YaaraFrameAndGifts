namespace YaaraFramesGifts.DTOs;

/// <summary>Product information returned by the API.</summary>
public sealed record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    string CategorySlug,
    string? Badge,
    bool IsPersonalized,
    CategoryDto Category);
