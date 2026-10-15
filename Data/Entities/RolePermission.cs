using Microsoft.AspNetCore.Identity;

namespace WfdHrms.Data.Entities;

public class RolePermission
{
    public int Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public IdentityRole Role { get; set; } = null!;

    public string PermissionName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
