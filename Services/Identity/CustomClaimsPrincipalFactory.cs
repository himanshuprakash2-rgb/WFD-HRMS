using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WfdHrms.Data;
using WfdHrms.Data.Entities;

namespace WfdHrms.Services.Identity;

public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CustomClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDbContextFactory<AppDbContext> contextFactory)
        : base(userManager, roleManager, optionsAccessor)
    {
        _contextFactory = contextFactory;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add display name claim
        if (!string.IsNullOrEmpty(user.FullName))
        {
            identity.AddClaim(new Claim("FullName", user.FullName));
        }

        // Dynamically load permissions based on assigned roles
        var userRoles = await UserManager.GetRolesAsync(user);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var permissions = await context.RolePermissions
            .Where(rp => context.Roles.Any(r => userRoles.Contains(r.Name!) && r.Id == rp.RoleId))
            .Select(rp => rp.PermissionName)
            .Distinct()
            .ToListAsync();

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(CustomClaimTypes.Permission, permission));
        }

        return identity;
    }
}
