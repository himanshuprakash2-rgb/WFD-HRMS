using Microsoft.AspNetCore.Components;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.Branches;

public partial class BranchList : ComponentBase
{
    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private List<Branch> branches = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        branches = await MasterDataService.GetAllBranchesAsync();
    }

    private async Task ToggleActiveAsync(int id)
    {
        var success = await MasterDataService.ToggleBranchActiveAsync(id);
        if (success)
        {
            ToastService.ShowSuccess("Branch status updated.");
            await LoadBranchesAsync();
        }
        else
        {
            ToastService.ShowError("Failed to update branch status.");
        }
    }
}
