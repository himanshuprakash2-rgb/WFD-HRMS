using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using WfdHrms.Components.Pages.Onboarding.Models;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Onboarding;

public partial class OnboardingList : ComponentBase
{
    [Inject] private IOnboardingService OnboardingService { get; set; } = null!;
    [Inject] private IMasterDataService MasterDataService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private ClaimsPrincipal currentUser = new();
    private List<OnboardingRequestVm> requests = new();
    private List<Branch> branches = new();
    private OnboardingFilterDto filter = new() { Status = "ALL" };
    private bool isLoading = true;

    private bool showDeleteConfirm = false;
    private OnboardingRequestVm? requestToDelete;

    private readonly Dictionary<string, string> statusTabs = new()
    {
        { "ALL", "All Requests" },
        { OnboardingStatus.Draft, "Drafts" },
        { OnboardingStatus.SentForApproval, "Sent for Approval" },
        { OnboardingStatus.RevisionRequired, "Revision Required" },
        { OnboardingStatus.Approved, "Approved" },
        { OnboardingStatus.Rejected, "Rejected" }
    };

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        currentUser = authState.User;

        branches = await MasterDataService.GetAllBranchesAsync(activeOnly: true);
        await LoadRequestsAsync();
    }

    private async Task LoadRequestsAsync()
    {
        isLoading = true;
        try
        {
            requests = await OnboardingService.GetRequestsAsync(currentUser, filter);
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to load requests: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task FilterByStatus(string status)
    {
        filter.Status = status;
        await LoadRequestsAsync();
    }

    private async Task ApplyFilterAsync()
    {
        await LoadRequestsAsync();
    }

    private void PromptDelete(OnboardingRequestVm request)
    {
        requestToDelete = request;
        showDeleteConfirm = true;
    }

    private async Task ConfirmDeleteAsync(string _)
    {
        if (requestToDelete == null) return;

        var result = await OnboardingService.DeleteRequestAsync(requestToDelete.Id, currentUser);
        showDeleteConfirm = false;

        if (!result.Success)
        {
            ToastService.ShowError(result.ErrorMessage ?? "Failed to delete request.");
            return;
        }

        ToastService.ShowSuccess($"Request for {requestToDelete.FullName} was deleted successfully.");
        requestToDelete = null;
        await LoadRequestsAsync();
    }
}
