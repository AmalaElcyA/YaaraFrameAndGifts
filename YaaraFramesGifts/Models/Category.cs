using System.ComponentModel.DataAnnotations.Schema;

namespace YaaraFramesGifts.Models;

public sealed class Category
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? AccentColor { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();

    [NotMapped]
    public string Id
    {
        get => Slug;
        set => Slug = value;
    }

    public string? ImageUrl { get; set; }

    public Category()
    {
    }

    public Category(string slug, string name, string? description, string imageUrl)
    {
        Slug = slug;
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
    }
}
