using System.ComponentModel.DataAnnotations;

namespace YaaraFramesGifts.DTOs;

/// <summary>Payload reserved for creating a category when catalog write access is enabled.</summary>
public sealed record CreateCategoryRequest
{
    /// <summary>Stable URL-friendly category key.</summary>
    [Required, StringLength(64, MinimumLength = 1)]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must use lowercase letters, numbers, and single hyphens.")]
    public required string Slug { get; init; }

    /// <summary>Display name of the category.</summary>
    [Required, StringLength(120, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>Optional category description.</summary>
    [StringLength(300)]
    public string? Description { get; init; }

    /// <summary>Optional short icon identifier.</summary>
    [StringLength(32)]
    public string? Icon { get; init; }

    /// <summary>Optional accent color.</summary>
    [StringLength(16)]
    [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", ErrorMessage = "Accent color must be a valid hexadecimal color.")]
    public string? AccentColor { get; init; }

    /// <summary>Optional absolute URL of the category image.</summary>
    [Url(ErrorMessage = "ImageUrl must be a valid absolute URL."), StringLength(500)]
    public string? ImageUrl { get; init; }
}
