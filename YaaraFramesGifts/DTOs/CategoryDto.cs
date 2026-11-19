namespace YaaraFramesGifts.DTOs;

/// <summary>Category information returned by the API.</summary>
public sealed record CategoryDto(
    string Slug,
    string Name,
    string? Description,
    string? Icon,
    string? AccentColor);
