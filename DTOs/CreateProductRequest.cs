using System.ComponentModel.DataAnnotations;

namespace YaaraFramesGifts.DTOs;

/// <summary>Payload reserved for creating a product when catalog write access is enabled.</summary>
public sealed record CreateProductRequest
{
    /// <summary>Product display name.</summary>
    [Required, StringLength(160, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>Product description.</summary>
    [Required, StringLength(1000, MinimumLength = 1)]
    public required string Description { get; init; }

    /// <summary>Price in Indian rupees.</summary>
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; init; }

    /// <summary>Absolute URL of the product image.</summary>
    [Required, Url(ErrorMessage = "ImageUrl must be a valid absolute URL."), StringLength(500, MinimumLength = 1)]
    public required string ImageUrl { get; init; }

    /// <summary>Slug of the product's category.</summary>
    [Required, StringLength(64, MinimumLength = 1)]
    public required string CategorySlug { get; init; }

    /// <summary>Optional product badge.</summary>
    [StringLength(32)]
    [RegularExpression("^(New|Popular|Recommended)$", ErrorMessage = "Badge must be New, Popular, Recommended, or null.")]
    public string? Badge { get; init; }

    /// <summary>Whether the product can be personalized.</summary>
    public bool IsPersonalized { get; init; }
}
