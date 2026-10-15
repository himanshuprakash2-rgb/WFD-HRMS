using Microsoft.AspNetCore.Components;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.Rbac;

public partial class RoleCreate : ComponentBase
{
    [Inject] private IRbacService RbacService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private string newRoleName = string.Empty;
    private bool isSaving = false;

    private async Task CreateRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(newRoleName))
        {
            ToastService.ShowError("Role name cannot be empty.");
            return;
        }

        try
        {
            isSaving = true;
            var trimmedName = newRoleName.Trim();
            var result = await RbacService.CreateRoleAsync(trimmedName);

            if (!result.Success)
            {
                ToastService.ShowError(result.Error ?? "Failed to create role.");
                return;
            }

            ToastService.ShowSuccess($"Role '{trimmedName}' created successfully.");
            newRoleName = string.Empty;
            Navigation.NavigateTo("/admin/roles");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }
}
