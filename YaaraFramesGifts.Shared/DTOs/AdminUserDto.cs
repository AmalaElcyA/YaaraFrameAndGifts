namespace YaaraFramesGifts.DTOs;

public sealed record AdminUserDto(
    int Id,
    string Username,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate);
