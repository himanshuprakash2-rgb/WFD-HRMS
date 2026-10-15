using Microsoft.AspNetCore.Components;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.PayrollFirms;

public partial class PayrollFirmList : ComponentBase
{
    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private List<PayrollFirm> payrollFirms = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadPayrollFirmsAsync();
    }

    private async Task LoadPayrollFirmsAsync()
    {
        payrollFirms = await MasterDataService.GetAllPayrollFirmsAsync();
    }

    private async Task ToggleActiveAsync(int id)
    {
        var success = await MasterDataService.TogglePayrollFirmActiveAsync(id);
        if (success)
        {
            ToastService.ShowSuccess("Payroll firm status updated.");
            await LoadPayrollFirmsAsync();
        }
        else
        {
            ToastService.ShowError("Failed to update payroll firm status.");
        }
    }
}
