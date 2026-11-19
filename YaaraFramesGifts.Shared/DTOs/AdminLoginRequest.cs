using System.ComponentModel.DataAnnotations;

namespace YaaraFramesGifts.DTOs;

public sealed record AdminLoginRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Username { get; init; }

    [Required, StringLength(128, MinimumLength = 1)]
    public required string Password { get; init; }
}
