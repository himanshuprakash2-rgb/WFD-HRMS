using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.Rbac;

public partial class RoleList : ComponentBase
{
    [Inject] private IRbacService RbacService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private List<IdentityRole> roles = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        roles = await RbacService.GetAllRolesAsync();
    }

    private async Task DeleteRoleAsync(string roleId)
    {
        var result = await RbacService.DeleteRoleAsync(roleId);
        if (!result.Success)
        {
            ToastService.ShowError(result.Error ?? "Failed to delete role.");
            return;
        }

        ToastService.ShowSuccess("Role deleted successfully.");
        await LoadRolesAsync();
    }
}
