using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WfdHrms.Data;
using WfdHrms.Data.Entities;

namespace WfdHrms.Services;

public class RbacService : IRbacService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RbacService> _logger;

    public RbacService(
        IDbContextFactory<AppDbContext> contextFactory,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<RbacService> logger)
    {
        _contextFactory = contextFactory;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<List<IdentityRole>> GetAllRolesAsync()
    {
        return await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateRoleAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return (false, "Role name cannot be empty.");

        var exists = await _roleManager.RoleExistsAsync(roleName.Trim());
        if (exists)
            return (false, $"Role '{roleName}' already exists.");

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteRoleAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return (false, "Role not found.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var permissions = await context.RolePermissions.Where(p => p.RoleId == roleId).ToListAsync();
        context.RolePermissions.RemoveRange(permissions);
        await context.SaveChangesAsync();

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<List<string>> GetPermissionsForRoleAsync(string roleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionName)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<string>>> GetAllRolePermissionsMapAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var all = await context.RolePermissions.AsNoTracking().ToListAsync();
        return all
            .GroupBy(rp => rp.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PermissionName).ToList());
    }

    public async Task<(bool Success, string? Error)> UpdateRolePermissionsAsync(string roleId, List<string> permissionNames)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            context.RolePermissions.RemoveRange(existing);

            foreach (var perm in permissionNames.Distinct())
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionName = perm,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role permissions for role {RoleId}", roleId);
            return (false, ex.Message);
        }
    }

    public async Task<List<UserDto>> GetAllUsersWithRolesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var users = await context.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            result.Add(new UserDto(user.Id, user.UserName ?? "", user.Email ?? "", user.FullName, user.IsActive, roles));
        }

        return result;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return new UserDto(user.Id, user.UserName ?? "", user.Email ?? "", user.FullName, user.IsActive, roles);
    }

    public async Task<List<string>> GetRolesForUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return [];
        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task<(bool Success, string? Error)> UpdateUserRolesAsync(string userId, List<string> roleNames)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "User not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            return (false, string.Join(", ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await _userManager.AddToRolesAsync(user, roleNames);
        if (!addResult.Succeeded)
            return (false, string.Join(", ", addResult.Errors.Select(e => e.Description)));

        return (true, null);
    }
}
