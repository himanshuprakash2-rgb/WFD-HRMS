using Microsoft.AspNetCore.Identity;
using WfdHrms.Data.Entities;

namespace WfdHrms.Services;

public record UserDto(string Id, string UserName, string Email, string FullName, bool IsActive, List<string> Roles);

public interface IRbacService
{
    // Roles
    Task<List<IdentityRole>> GetAllRolesAsync();
    Task<(bool Success, string? Error)> CreateRoleAsync(string roleName);
    Task<(bool Success, string? Error)> DeleteRoleAsync(string roleId);

    // Permissions
    Task<List<string>> GetPermissionsForRoleAsync(string roleId);
    Task<Dictionary<string, List<string>>> GetAllRolePermissionsMapAsync();
    Task<(bool Success, string? Error)> UpdateRolePermissionsAsync(string roleId, List<string> permissionNames);

    // User Roles
    Task<List<UserDto>> GetAllUsersWithRolesAsync();
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<List<string>> GetRolesForUserAsync(string userId);
    Task<(bool Success, string? Error)> UpdateUserRolesAsync(string userId, List<string> roleNames);
}
