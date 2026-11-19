using System.ComponentModel.DataAnnotations;

namespace YaaraFramesGifts.DTOs;

public sealed record CreateAdminUserRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public required string Username { get; init; }

    [Required, StringLength(128, MinimumLength = 12)]
    public required string Password { get; init; }

    [Required, StringLength(160, MinimumLength = 1)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }
}
