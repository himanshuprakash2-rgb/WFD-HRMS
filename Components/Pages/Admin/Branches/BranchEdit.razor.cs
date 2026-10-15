using Microsoft.AspNetCore.Components;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.Branches;

public partial class BranchEdit : ComponentBase
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private Branch branch = new() { IsActive = true };
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        if (Id.HasValue && Id.Value > 0)
        {
            var existing = await MasterDataService.GetBranchByIdAsync(Id.Value);
            if (existing == null)
            {
                ToastService.ShowError("Branch not found.");
                Navigation.NavigateTo("/admin/branches");
                return;
            }

            branch = new Branch
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
            branch = new Branch { IsActive = true };
        }
        isLoading = false;
    }

    private async Task SaveBranchAsync()
    {
        if (string.IsNullOrWhiteSpace(branch.Name) || string.IsNullOrWhiteSpace(branch.Code))
        {
            ToastService.ShowError("Branch Name and Code are required.");
            return;
        }

        try
        {
            isSaving = true;
            branch.Code = branch.Code.Trim().ToUpperInvariant();
            branch.Name = branch.Name.Trim();

            var isNew = branch.Id == 0;
            var result = await MasterDataService.SaveBranchAsync(branch);

            if (!result.Success)
            {
                ToastService.ShowError(result.Error ?? "Failed to save branch.");
                return;
            }

            ToastService.ShowSuccess(isNew ? "Branch created successfully." : "Branch updated successfully.");

            if (isNew && branch.Id > 0)
            {
                Id = branch.Id;
                Navigation.NavigateTo($"/admin/branches/edit/{branch.Id}", replace: true);
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
