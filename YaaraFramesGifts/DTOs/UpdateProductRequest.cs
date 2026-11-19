using System.ComponentModel.DataAnnotations;

namespace YaaraFramesGifts.DTOs;

/// <summary>Payload reserved for updating a product when catalog write access is enabled.</summary>
public sealed record UpdateProductRequest
{
    /// <summary>Updated product display name.</summary>
    [Required, StringLength(160, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>Updated product description.</summary>
    [Required, StringLength(1000, MinimumLength = 1)]
    public required string Description { get; init; }

    /// <summary>Updated price in Indian rupees.</summary>
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; init; }

    /// <summary>Absolute URL of the updated product image.</summary>
    [Required, Url(ErrorMessage = "ImageUrl must be a valid absolute URL."), StringLength(500, MinimumLength = 1)]
    public required string ImageUrl { get; init; }

    /// <summary>Slug of the product's updated category.</summary>
    [Required, StringLength(64, MinimumLength = 1)]
    public required string CategorySlug { get; init; }

    /// <summary>Optional updated product badge.</summary>
    [StringLength(32)]
    [RegularExpression("^(New|Popular|Recommended)$", ErrorMessage = "Badge must be New, Popular, Recommended, or null.")]
    public string? Badge { get; init; }

    /// <summary>Whether the product can be personalized.</summary>
    public bool IsPersonalized { get; init; }
}
