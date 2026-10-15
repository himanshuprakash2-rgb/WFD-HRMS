using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.Rbac;

public partial class RolePermissions : ComponentBase
{
    [Inject] private IRbacService RbacService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private List<IdentityRole> roles = new();
    private Dictionary<string, HashSet<string>> matrix = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadMatrixAsync();
    }

    private async Task LoadMatrixAsync()
    {
        roles = await RbacService.GetAllRolesAsync();
        var map = await RbacService.GetAllRolePermissionsMapAsync();

        matrix = new Dictionary<string, HashSet<string>>();
        foreach (var r in roles)
        {
            if (map.TryGetValue(r.Id, out var perms))
                matrix[r.Id] = perms.ToHashSet();
            else
                matrix[r.Id] = new HashSet<string>();
        }
    }

    private void TogglePermission(string roleId, string permission, bool isChecked)
    {
        if (!matrix.ContainsKey(roleId))
            matrix[roleId] = new HashSet<string>();

        if (isChecked)
            matrix[roleId].Add(permission);
        else
            matrix[roleId].Remove(permission);
    }

    private async Task SaveMatrixAsync()
    {
        foreach (var role in roles)
        {
            var perms = matrix.TryGetValue(role.Id, out var set) ? set.ToList() : new List<string>();
            var result = await RbacService.UpdateRolePermissionsAsync(role.Id, perms);
            if (!result.Success)
            {
                ToastService.ShowError(result.Error ?? $"Error saving permissions for role {role.Name}.");
                return;
            }
        }

        ToastService.ShowSuccess("All role permissions updated successfully. Users will receive updated claims upon next sign-in.");
    }
}
