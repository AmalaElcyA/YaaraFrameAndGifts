using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Security;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Admin.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(AdminAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        AdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null
            ? Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sign-in failed",
                Detail = "The username or password is invalid, or the account is inactive."
            })
            : Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Policy = AdminAuthenticationDefaults.Policy)]
    public async Task<ActionResult<AdminUserDto>> GetCurrent(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            return Unauthorized();
        }

        var admin = await authService.GetUserAsync(id, cancellationToken);
        return admin is null ? Unauthorized() : Ok(admin);
    }

    [HttpPost("logout")]
    [Authorize(Policy = AdminAuthenticationDefaults.Policy)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            await authService.RevokeTokenAsync(id, cancellationToken);
        }

        return NoContent();
    }
}
