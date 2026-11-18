namespace YaaraFramesGifts.Models;

public sealed record Product(
    string Id,
    string Name,
    Category Category,
    string Description,
    decimal Price,
    string ImageUrl,
    string? Badge = null,
    IReadOnlyList<string>? GalleryImages = null,
    bool IsAvailable = true);
