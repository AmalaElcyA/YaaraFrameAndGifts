using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YaaraFramesGifts.Data;
using YaaraFramesGifts.DTOs;
using YaaraFramesGifts.Models;
using YaaraFramesGifts.Security;

namespace YaaraFramesGifts.Services;

public sealed class AdminAuthService(
    YaaraDbContext dbContext,
    IPasswordHasher<AdminUser> passwordHasher,
    AdminAccessTokenService tokenService)
{
    public async Task<AdminLoginResponse?> LoginAsync(
        AdminLoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = NormalizeUsername(request.Username);
        var admin = await dbContext.AdminUsers
            .SingleOrDefaultAsync(user => user.NormalizedUsername == normalizedUsername, cancellationToken);
        if (admin is null || !admin.IsActive)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            admin.PasswordHash = passwordHasher.HashPassword(admin, request.Password);
            admin.UpdatedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AdminLoginResponse(
            tokenService.CreateToken(admin),
            AdminAuthenticationDefaults.AccessTokenLifetimeSeconds,
            Map(admin));
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken) =>
        await dbContext.AdminUsers
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Select(user => new AdminUserDto(
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.IsActive,
                user.CreatedDate,
                user.UpdatedDate))
            .ToListAsync(cancellationToken);

    public async Task<AdminUserDto?> GetUserAsync(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(admin => admin.Id == id, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task RevokeTokenAsync(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.AdminUsers
            .SingleOrDefaultAsync(admin => admin.Id == id, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminUserCreateResult> CreateUserAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = NormalizeUsername(username);
        if (await dbContext.AdminUsers.AnyAsync(
                user => user.NormalizedUsername == normalizedUsername,
                cancellationToken))
        {
            return new AdminUserCreateResult(AdminUserCreateStatus.UsernameAlreadyExists, null);
        }

        if (!AdminPasswordPolicy.IsStrong(request.Password))
        {
            return new AdminUserCreateResult(AdminUserCreateStatus.PasswordPolicyFailed, null);
        }

        var now = DateTime.UtcNow;
        var admin = new AdminUser
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, request.Password);

        dbContext.AdminUsers.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdminUserCreateResult(AdminUserCreateStatus.Success, Map(admin));
    }

    public async Task<AdminUserStatusResult> SetActiveStatusAsync(
        int id,
        bool isActive,
        int currentAdminId,
        CancellationToken cancellationToken)
    {
        var admin = await dbContext.AdminUsers
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        if (admin is null)
        {
            return AdminUserStatusResult.NotFound;
        }

        if (!isActive && admin.Id == currentAdminId)
        {
            return AdminUserStatusResult.CannotDeactivateSelf;
        }

        if (!isActive && admin.IsActive
            && await dbContext.AdminUsers.CountAsync(user => user.IsActive, cancellationToken) <= 1)
        {
            return AdminUserStatusResult.CannotDeactivateLastAdmin;
        }

        if (admin.IsActive != isActive)
        {
            admin.IsActive = isActive;
            admin.SecurityStamp = Guid.NewGuid().ToString("N");
            admin.UpdatedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return AdminUserStatusResult.Success;
    }

    public async Task<bool> EnsureBootstrapUserAsync(
        string username,
        string password,
        string fullName,
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (await dbContext.AdminUsers.AnyAsync(
                user => user.NormalizedUsername == normalizedUsername,
                cancellationToken))
        {
            return false;
        }

        if (!AdminPasswordPolicy.IsStrong(password))
        {
            throw new InvalidOperationException(
                "The configured bootstrap password must be at least 12 characters and include lowercase, uppercase, numeric, and non-alphanumeric characters.");
        }

        var now = DateTime.UtcNow;
        var admin = new AdminUser
        {
            Username = username.Trim(),
            NormalizedUsername = normalizedUsername,
            FullName = fullName.Trim(),
            Email = email.Trim(),
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        dbContext.AdminUsers.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static AdminUserDto Map(AdminUser user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Email,
        user.IsActive,
        user.CreatedDate,
        user.UpdatedDate);

    public static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();
}

public static class AdminPasswordPolicy
{
    public static bool IsStrong(string password) =>
        password.Length >= 12
        && password.Any(char.IsLower)
        && password.Any(char.IsUpper)
        && password.Any(char.IsDigit)
        && password.Any(character => !char.IsLetterOrDigit(character));
}

public sealed record AdminUserCreateResult(AdminUserCreateStatus Status, AdminUserDto? User);

public enum AdminUserCreateStatus
{
    Success,
    UsernameAlreadyExists,
    PasswordPolicyFailed
}

public enum AdminUserStatusResult
{
    Success,
    NotFound,
    CannotDeactivateSelf,
    CannotDeactivateLastAdmin
}
