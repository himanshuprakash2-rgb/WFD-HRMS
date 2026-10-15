using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using WfdHrms.Components.Pages.Onboarding.Models;
using WfdHrms.Data.Entities;
using WfdHrms.Services;
using WfdHrms.Services.StateMachine;

namespace WfdHrms.Components.Pages.Onboarding;

public partial class OnboardingReview : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IOnboardingService OnboardingService { get; set; } = null!;
    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private ClaimsPrincipal currentUser = new();
    private OnboardingRequestVm model = new();
    private List<PayrollFirm> mappedPayrollFirms = new();
    private List<AuditLog> auditLogs = new();
    private int selectedPayrollFirmId = 0;

    // Action Dialog state
    private bool showActionDialog = false;
    private string currentAction = string.Empty;
    private string dialogTitle = string.Empty;
    private string dialogMessage = string.Empty;
    private string dialogConfirmText = string.Empty;
    private string dialogConfirmClass = string.Empty;
    private bool dialogRequireInput = false;

    private bool showDeleteConfirm = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUser = authState.User;

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var req = await OnboardingService.GetRequestByIdAsync(Id, currentUser);
        if (req == null)
        {
            ToastService.ShowError("Onboarding request not found.");
            Navigation.NavigateTo("/onboarding");
            return;
        }

        model = req;
        selectedPayrollFirmId = model.SelectedPayrollFirmId;

        mappedPayrollFirms = await MasterDataService.GetValidPayrollFirmsForBranchAsync(model.BranchId);
        auditLogs = await OnboardingService.GetAuditLogsAsync(Id);
    }

    private async Task OverridePayrollAsync()
    {
        if (selectedPayrollFirmId == 0 || selectedPayrollFirmId == model.SelectedPayrollFirmId)
            return;

        var result = await OnboardingService.OverridePayrollAndEditAsync(
            Id,
            selectedPayrollFirmId,
            model,
            currentUser);

        if (!result.Success)
        {
            ToastService.ShowError(result.ErrorMessage ?? "Failed to override payroll firm.");
            return;
        }

        ToastService.ShowSuccess("Payroll firm overridden successfully.");
        await LoadDataAsync();
    }

    private void OpenApproveDialog()
    {
        currentAction = WorkflowActions.Approve;
        dialogTitle = "Approve Candidate & Generate Employee Code";
        dialogMessage = $"Confirm approval for {model.FullName}. An employee record will be created and an atomic employee code will be generated permanently.";
        dialogConfirmText = "Approve & Lock Record";
        dialogConfirmClass = "bg-emerald-600 hover:bg-emerald-700";
        dialogRequireInput = false;
        showActionDialog = true;
    }

    private void OpenRevisionDialog()
    {
        currentAction = WorkflowActions.RequestRevision;
        dialogTitle = "Request Revision from Supervisor";
        dialogMessage = $"Please enter specific revision instructions for the supervisor to update candidate {model.FullName}.";
        dialogConfirmText = "Send Revision Request";
        dialogConfirmClass = "bg-amber-600 hover:bg-amber-700";
        dialogRequireInput = true;
        showActionDialog = true;
    }

    private void OpenRejectDialog()
    {
        currentAction = WorkflowActions.Reject;
        dialogTitle = "Reject Onboarding Request";
        dialogMessage = $"Please state the mandatory reason for rejecting candidate {model.FullName}.";
        dialogConfirmText = "Reject Candidate";
        dialogConfirmClass = "bg-rose-600 hover:bg-rose-700";
        dialogRequireInput = true;
        showActionDialog = true;
    }

    private async Task HandleDialogConfirmedAsync(string remarks)
    {
        showActionDialog = false;

        if (currentAction == WorkflowActions.Approve)
        {
            var result = await OnboardingService.ApproveRequestAsync(Id, currentUser);
            if (!result.Success)
            {
                ToastService.ShowError(result.ErrorMessage ?? "Approval failed.");
                return;
            }

            ToastService.ShowSuccess($"Request Approved! Employee Code '{result.EmployeeCode}' generated successfully.");
            await LoadDataAsync();
        }
        else if (currentAction == WorkflowActions.RequestRevision)
        {
            var result = await OnboardingService.RequestRevisionAsync(Id, remarks, currentUser);
            if (!result.Success)
            {
                ToastService.ShowError(result.ErrorMessage ?? "Revision request failed.");
                return;
            }

            ToastService.ShowSuccess("Revision request sent to the supervisor.");
            await LoadDataAsync();
        }
        else if (currentAction == WorkflowActions.Reject)
        {
            var result = await OnboardingService.RejectRequestAsync(Id, remarks, currentUser);
            if (!result.Success)
            {
                ToastService.ShowError(result.ErrorMessage ?? "Rejection failed.");
                return;
            }

            ToastService.ShowSuccess("Onboarding request has been rejected.");
            await LoadDataAsync();
        }
    }

    private async Task HandleDeleteConfirmedAsync(string _)
    {
        showDeleteConfirm = false;
        var result = await OnboardingService.DeleteRequestAsync(Id, currentUser);
        if (!result.Success)
        {
            ToastService.ShowError(result.ErrorMessage ?? "Failed to delete request.");
            return;
        }

        ToastService.ShowSuccess("Onboarding request deleted successfully.");
        Navigation.NavigateTo("/onboarding");
    }
}
