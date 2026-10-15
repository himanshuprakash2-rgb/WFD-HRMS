using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.Rbac;

public partial class UserRoleEdit : ComponentBase
{
    [Parameter] public string UserId { get; set; } = string.Empty;

    [Inject] private IRbacService RbacService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private UserDto? user;
    private List<IdentityRole> allRoles = new();
    private HashSet<string> selectedRoles = new();
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        allRoles = await RbacService.GetAllRolesAsync();
        user = await RbacService.GetUserByIdAsync(UserId);

        if (user == null)
        {
            ToastService.ShowError("User not found.");
            Navigation.NavigateTo("/admin/user-roles");
            return;
        }

        selectedRoles = new HashSet<string>(user.Roles);
        isLoading = false;
    }

    private void ToggleRole(string roleName, bool isChecked)
    {
        if (isChecked)
        {
            selectedRoles.Add(roleName);
        }
        else
        {
            selectedRoles.Remove(roleName);
        }
    }

    private async Task SaveUserRolesAsync()
    {
        if (user == null) return;

        try
        {
            isSaving = true;
            var result = await RbacService.UpdateUserRolesAsync(user.Id, selectedRoles.ToList());

            if (!result.Success)
            {
                ToastService.ShowError(result.Error ?? "Failed to update user roles.");
                return;
            }

            ToastService.ShowSuccess($"Roles updated successfully for {user.FullName}.");
            user = await RbacService.GetUserByIdAsync(user.Id);
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
