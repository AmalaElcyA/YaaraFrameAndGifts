using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Security;
using YaaraFramesGifts.Services;

namespace YaaraFramesGifts.Admin.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AdminAuthenticationDefaults.Policy)]
public sealed class AdminUsersController(AdminAuthService authService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await authService.GetUsersAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminUserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await authService.GetUserAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.CreateUserAsync(request, cancellationToken);
        return result.Status switch
        {
            AdminUserCreateStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.User!.Id },
                result.User),
            AdminUserCreateStatus.UsernameAlreadyExists => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Username already exists",
                Detail = "Choose a unique username."
            }),
            _ => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Password)] =
                [
                    "Use at least 12 characters with lowercase, uppercase, numeric, and non-alphanumeric characters."
                ]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Password does not meet the security policy."
            })
        };
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(
        int id,
        AdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentAdminId))
        {
            return Unauthorized();
        }

        var result = await authService.SetActiveStatusAsync(
            id,
            request.IsActive,
            currentAdminId,
            cancellationToken);
        return result switch
        {
            AdminUserStatusResult.Success => NoContent(),
            AdminUserStatusResult.NotFound => NotFound(),
            AdminUserStatusResult.CannotDeactivateSelf => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Cannot deactivate your own account",
                Detail = "Sign in with another active administrator to deactivate this account."
            }),
            _ => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Cannot deactivate the last active administrator",
                Detail = "At least one active administrator must remain."
            })
        };
    }
}
