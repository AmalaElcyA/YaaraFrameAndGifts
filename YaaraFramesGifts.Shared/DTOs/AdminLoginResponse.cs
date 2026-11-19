namespace YaaraFramesGifts.DTOs;

public sealed record AdminLoginResponse(string AccessToken, int ExpiresIn, AdminUserDto User);
