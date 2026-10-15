using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WfdHrms.Data.Entities;
using WfdHrms.Services.Identity;

namespace WfdHrms.Data.Seeding;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles & Granular Permissions
        var rolesWithPerms = new Dictionary<string, string[]>
        {
            {
                "SystemAdmin",
                AppPermissions.All
            },
            {
                "WorkforceManager",
                [
                    AppPermissions.CanApproveOnboarding,
                    AppPermissions.CanOverridePayroll,
                    AppPermissions.CanDeleteUnapprovedRequest
                ]
            },
            {
                "BranchSupervisor",
                [AppPermissions.CanCreateOnboarding]
            }
        };

        foreach (var (roleName, perms) in rolesWithPerms)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new IdentityRole(roleName);
                await roleManager.CreateAsync(role);
            }

            foreach (var perm in perms)
            {
                var hasPerm = await context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionName == perm);

                if (!hasPerm)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionName = perm,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
        await context.SaveChangesAsync();


        await context.SaveChangesAsync();

        // 3. Seed ONLY the single Super Administrator account (Himanshu Prakash)
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@wfdhrms.com",
                FullName = "Himanshu Prakash",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, "Password@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SystemAdmin");
            }
        }
        else
        {
            adminUser.FullName = "Himanshu Prakash";
            adminUser.Email = "admin@wfdhrms.com";
            await userManager.UpdateAsync(adminUser);
            if (!await userManager.IsInRoleAsync(adminUser, "SystemAdmin"))
            {
                await userManager.AddToRoleAsync(adminUser, "SystemAdmin");
            }
        }

        // 4. Default Employee Code Template (pattern ready for when branches are created)
        if (!await context.EmployeeCodeConfigs.AnyAsync())
        {
            context.EmployeeCodeConfigs.Add(new EmployeeCodeConfig
            {
                BranchId = null,
                TemplatePattern = "EMP-{BRANCH}-{YEAR}-{SEQ:4}",
                CurrentSequence = 0,
                LastResetYear = DateTime.UtcNow.Year,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
