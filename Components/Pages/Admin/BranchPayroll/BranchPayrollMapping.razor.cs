using Microsoft.AspNetCore.Components;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.BranchPayroll;

public partial class BranchPayrollMapping : ComponentBase
{
    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private List<Branch> branches = new();
    private List<PayrollFirm> allPayrollFirms = new();
    private int selectedBranchId = 0;
    private HashSet<int> selectedFirmIds = new();

    protected override async Task OnInitializedAsync()
    {
        branches = await MasterDataService.GetAllBranchesAsync(activeOnly: true);
        allPayrollFirms = await MasterDataService.GetAllPayrollFirmsAsync();
    }

    private async Task OnBranchSelectedAsync()
    {
        selectedFirmIds.Clear();

        if (selectedBranchId > 0)
        {
            var currentMappings = await MasterDataService.GetMappingsForBranchAsync(selectedBranchId);
            selectedFirmIds = currentMappings.Where(m => m.IsActive).Select(m => m.PayrollFirmId).ToHashSet();
        }
    }

    private void ToggleFirmSelection(int firmId, bool isChecked)
    {
        if (isChecked)
            selectedFirmIds.Add(firmId);
        else
            selectedFirmIds.Remove(firmId);
    }

    private async Task SaveMappingsAsync()
    {
        if (selectedBranchId == 0)
        {
            ToastService.ShowWarning("Please select a branch first.");
            return;
        }

        var result = await MasterDataService.UpdateBranchPayrollMappingsAsync(selectedBranchId, selectedFirmIds.ToList());
        if (!result.Success)
        {
            ToastService.ShowError(result.Error ?? "Failed to update branch payroll mappings.");
            return;
        }

        ToastService.ShowSuccess("Branch & Payroll Firm mappings updated successfully.");
    }
}
