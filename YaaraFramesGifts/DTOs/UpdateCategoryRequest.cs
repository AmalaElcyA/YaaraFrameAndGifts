using System.ComponentModel.DataAnnotations;

namespace YaaraFramesGifts.DTOs;

/// <summary>Payload reserved for updating a category when catalog write access is enabled.</summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>Updated display name.</summary>
    [Required, StringLength(120, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>Optional updated description.</summary>
    [StringLength(300)]
    public string? Description { get; init; }

    /// <summary>Optional updated icon identifier.</summary>
    [StringLength(32)]
    public string? Icon { get; init; }

    /// <summary>Optional updated accent color.</summary>
    [StringLength(16)]
    [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", ErrorMessage = "Accent color must be a valid hexadecimal color.")]
    public string? AccentColor { get; init; }

    /// <summary>Optional updated absolute URL of the category image.</summary>
    [Url(ErrorMessage = "ImageUrl must be a valid absolute URL."), StringLength(500)]
    public string? ImageUrl { get; init; }
}
