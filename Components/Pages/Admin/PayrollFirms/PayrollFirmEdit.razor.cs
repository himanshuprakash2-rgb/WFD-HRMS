using Microsoft.AspNetCore.Components;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.PayrollFirms;

public partial class PayrollFirmEdit : ComponentBase
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private PayrollFirm payrollFirm = new() { IsActive = true };
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        if (Id.HasValue && Id.Value > 0)
        {
            var existing = await MasterDataService.GetPayrollFirmByIdAsync(Id.Value);
            if (existing == null)
            {
                ToastService.ShowError("Payroll firm not found.");
                Navigation.NavigateTo("/admin/payroll-firms");
                return;
            }

            payrollFirm = new PayrollFirm
            {
                Id = existing.Id,
                Name = existing.Name,
                Code = existing.Code,
                IsActive = existing.IsActive,
                CreatedAt = existing.CreatedAt
            };
        }
        else
        {
            payrollFirm = new PayrollFirm { IsActive = true };
        }
        isLoading = false;
    }

    private async Task SaveFirmAsync()
    {
        if (string.IsNullOrWhiteSpace(payrollFirm.Name) || string.IsNullOrWhiteSpace(payrollFirm.Code))
        {
            ToastService.ShowError("Firm Name and Code are required.");
            return;
        }

        try
        {
            isSaving = true;
            payrollFirm.Code = payrollFirm.Code.Trim().ToUpperInvariant();
            payrollFirm.Name = payrollFirm.Name.Trim();

            var isNew = payrollFirm.Id == 0;
            var result = await MasterDataService.SavePayrollFirmAsync(payrollFirm);

            if (!result.Success)
            {
                ToastService.ShowError(result.Error ?? "Failed to save payroll firm.");
                return;
            }

            ToastService.ShowSuccess(isNew ? "Payroll firm created successfully." : "Payroll firm updated successfully.");

            if (isNew && payrollFirm.Id > 0)
            {
                Id = payrollFirm.Id;
                Navigation.NavigateTo($"/admin/payroll-firms/edit/{payrollFirm.Id}", replace: true);
            }
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
