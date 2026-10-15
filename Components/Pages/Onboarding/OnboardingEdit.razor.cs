using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using WfdHrms.Components.Pages.Onboarding.Models;
using WfdHrms.Data.Entities;
using WfdHrms.Services;
using WfdHrms.Services.Identity;

namespace WfdHrms.Components.Pages.Onboarding;

public partial class OnboardingEdit : ComponentBase
{
    [Parameter] public Guid? Id { get; set; }

    [Inject] private IOnboardingService OnboardingService { get; set; } = null!;
    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private IFileUploadService FileUploadService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private ClaimsPrincipal currentUser = new();
    private OnboardingRequestVm model = new();
    private List<Branch> userBranches = new();
    private List<PayrollFirm> validPayrollFirms = new();

    private string? identityConflictWarning;
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUser = authState.User;
        var userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var isWorkforce = currentUser.HasClaim(CustomClaimTypes.Permission, AppPermissions.CanApproveOnboarding) ||
                          currentUser.HasClaim(CustomClaimTypes.Permission, AppPermissions.CanOverridePayroll);

        if (isWorkforce)
        {
            userBranches = await MasterDataService.GetAllBranchesAsync(activeOnly: true);
        }
        else
        {
            userBranches = await MasterDataService.GetAssignedBranchesForUserAsync(userId);
        }

        if (Id.HasValue && Id.Value != Guid.Empty)
        {
            var req = await OnboardingService.GetRequestByIdAsync(Id.Value, currentUser);
            if (req == null)
            {
                Navigation.NavigateTo("/onboarding");
                return;
            }
            model = req;
            await OnBranchChangedAsync();
        }
        else
        {
            model = new OnboardingRequestVm { Id = Guid.NewGuid() };
            if (userBranches.Count == 1)
            {
                model.BranchId = userBranches[0].Id;
                await OnBranchChangedAsync();
            }
        }
    }

    private async Task OnBranchChangedAsync()
    {
        validPayrollFirms.Clear();
        if (model.BranchId > 0)
        {
            validPayrollFirms = await MasterDataService.GetValidPayrollFirmsForBranchAsync(model.BranchId);
            if (validPayrollFirms.All(p => p.Id != model.SelectedPayrollFirmId))
            {
                model.SelectedPayrollFirmId = validPayrollFirms.Count == 1 ? validPayrollFirms[0].Id : 0;
            }
        }
        else
        {
            model.SelectedPayrollFirmId = 0;
        }
    }

    private async Task CheckDuplicateAndFraudAsync()
    {
        if (string.IsNullOrWhiteSpace(model.AadhaarNumber) || string.IsNullOrWhiteSpace(model.PanNumber))
        {
            identityConflictWarning = null;
            return;
        }

        var result = await OnboardingService.ValidateDuplicateAndFraudAsync(
            model.AadhaarNumber,
            model.PanNumber,
            model.Id == Guid.Empty ? null : model.Id);

        if (!result.Success)
        {
            identityConflictWarning = result.ErrorMessage;
        }
        else
        {
            identityConflictWarning = null;
        }
    }

    private bool HasAttachment(string documentType) =>
        model.Attachments.Any(a => a.DocumentType == documentType);

    private AttachmentVm? GetAttachment(string documentType) =>
        model.Attachments.FirstOrDefault(a => a.DocumentType == documentType);

    private async Task HandleFileSelected(InputFileChangeEventArgs e, string documentType)
    {
        var file = e.File;
        if (file == null) return;

        try
        {
            if (model.BranchId == 0)
            {
                ToastService.ShowError("Please select a branch before uploading documents.");
                return;
            }

            // If draft is not yet saved, save it first so foreign key exists
            if (model.Id == Guid.Empty)
            {
                var draftRes = await OnboardingService.SaveDraftAsync(model, currentUser);
                if (!draftRes.Success)
                {
                    ToastService.ShowError(draftRes.ErrorMessage ?? "Failed to save draft before uploading.");
                    return;
                }
                model.Id = draftRes.RequestId;
            }

            // Stream directly using native Blazor chunked streaming (max 10 MB)
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var uploadRes = await FileUploadService.UploadFileAsync(stream, file.Name, documentType);

            if (!uploadRes.Success)
            {
                ToastService.ShowError(uploadRes.ErrorMessage ?? "Upload failed.");
                return;
            }

            var saveAttRes = await OnboardingService.SaveAttachmentAsync(
                model.Id,
                documentType,
                uploadRes.FileId!,
                uploadRes.FileUrl!,
                uploadRes.FileSizeBytes,
                currentUser);

            if (!saveAttRes.Success)
            {
                ToastService.ShowError(saveAttRes.ErrorMessage ?? "Failed to save attachment metadata.");
                return;
            }

            // Refresh attachment in model
            model.Attachments.RemoveAll(a => a.DocumentType == documentType);
            model.Attachments.Add(new AttachmentVm
            {
                Id = Guid.NewGuid(),
                DocumentType = documentType,
                FileId = uploadRes.FileId!,
                FileUrl = uploadRes.FileUrl!,
                FileSizeBytes = uploadRes.FileSizeBytes
            });

            var label = documentType switch
            {
                DocumentTypes.AadhaarCard => "Aadhaar Card",
                DocumentTypes.PanCard => "PAN Card",
                DocumentTypes.BankPassbook => "Bank Passbook",
                _ => documentType
            };

            ToastService.ShowSuccess($"{label} uploaded successfully.");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"File upload failed: {ex.Message}");
        }
    }

    private async Task SaveDraftAsync()
    {
        isProcessing = true;

        try
        {
            if (model.BranchId == 0)
            {
                ToastService.ShowError("Please select a Branch before saving draft.");
                return;
            }

            var result = await OnboardingService.SaveDraftAsync(model, currentUser);
            if (!result.Success)
            {
                ToastService.ShowError(result.ErrorMessage ?? "Failed to save draft.");
                return;
            }

            model.Id = result.RequestId;
            ToastService.ShowSuccess("Onboarding draft saved successfully.");
        }
        finally
        {
            isProcessing = false;
        }
    }

    private async Task HandleSubmitAsync()
    {
        isProcessing = true;

        try
        {
            await CheckDuplicateAndFraudAsync();
            if (!string.IsNullOrEmpty(identityConflictWarning))
            {
                ToastService.ShowError(identityConflictWarning);
                return;
            }

            // Save draft first to persist any modified fields
            var draftRes = await OnboardingService.SaveDraftAsync(model, currentUser);
            if (!draftRes.Success)
            {
                ToastService.ShowError(draftRes.ErrorMessage ?? "Failed to save draft.");
                return;
            }
            model.Id = draftRes.RequestId;

            // Submit for approval
            var submitRes = await OnboardingService.SubmitRequestAsync(model.Id, currentUser);
            if (!submitRes.Success)
            {
                ToastService.ShowError(submitRes.ErrorMessage ?? "Failed to submit request.");
                return;
            }

            ToastService.ShowSuccess("Onboarding request submitted for approval.");
            Navigation.NavigateTo("/onboarding");
        }
        finally
        {
            isProcessing = false;
        }
    }
}
