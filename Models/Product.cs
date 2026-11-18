using System.ComponentModel.DataAnnotations.Schema;

namespace YaaraFramesGifts.Models;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string? Badge { get; set; }
    public bool IsPersonalized { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [NotMapped]
    public IReadOnlyList<string>? GalleryImages { get; set; }

    [NotMapped]
    public bool IsAvailable { get; set; } = true;

    public Product()
    {
    }

    public Product(
        int id,
        string name,
        Category category,
        string description,
        decimal price,
        string imageUrl,
        string? badge = null,
        IReadOnlyList<string>? galleryImages = null,
        bool isAvailable = true,
        bool isPersonalized = false)
    {
        Id = id;
        Name = name;
        Category = category;
        CategorySlug = category.Slug;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        Badge = badge;
        GalleryImages = galleryImages;
        IsAvailable = isAvailable;
        IsPersonalized = isPersonalized;
    }
}
