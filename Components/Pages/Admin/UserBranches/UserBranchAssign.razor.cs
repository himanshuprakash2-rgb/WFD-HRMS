using Microsoft.AspNetCore.Components;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.UserBranches;

public partial class UserBranchAssign : ComponentBase
{
    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private IRbacService RbacService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private List<UserDto> users = new();
    private List<Branch> allBranches = new();
    private string selectedUserId = string.Empty;
    private HashSet<int> selectedBranchIds = new();

    protected override async Task OnInitializedAsync()
    {
        users = await RbacService.GetAllUsersWithRolesAsync();
        allBranches = await MasterDataService.GetAllBranchesAsync();
    }

    private async Task OnUserSelectedAsync()
    {
        selectedBranchIds.Clear();

        if (!string.IsNullOrEmpty(selectedUserId))
        {
            var branchIds = await MasterDataService.GetAssignedBranchIdsForUserAsync(selectedUserId);
            selectedBranchIds = branchIds.ToHashSet();
        }
    }

    private void ToggleBranchSelection(int branchId, bool isChecked)
    {
        if (isChecked)
            selectedBranchIds.Add(branchId);
        else
            selectedBranchIds.Remove(branchId);
    }

    private async Task SaveAssignmentsAsync()
    {
        if (string.IsNullOrEmpty(selectedUserId))
        {
            ToastService.ShowWarning("Please select a user first.");
            return;
        }

        var result = await MasterDataService.UpdateUserBranchAssignmentsAsync(selectedUserId, selectedBranchIds.ToList());
        if (!result.Success)
        {
            ToastService.ShowError(result.Error ?? "Failed to update user-branch assignments.");
            return;
        }

        ToastService.ShowSuccess("User-Branch assignments updated successfully.");
    }
}
