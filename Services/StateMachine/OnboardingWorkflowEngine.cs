using System.Security.Claims;
using WfdHrms.Data.Entities;
using WfdHrms.Services.Identity;

namespace WfdHrms.Services.StateMachine;

public record StateTransitionRule(
    string CurrentState,
    string Action,
    string TargetState,
    string RequiredClaim
);

public static class WorkflowActions
{
    public const string Submit = "SUBMIT";
    public const string Resubmit = "RESUBMIT";
    public const string Approve = "APPROVE";
    public const string RequestRevision = "REQUEST_REVISION";
    public const string Reject = "REJECT";
    public const string OverridePayroll = "OVERRIDE_PAYROLL";
    public const string Delete = "DELETE";
}

public class OnboardingWorkflowEngine
{
    private static readonly List<StateTransitionRule> TransitionRules =
    [
        new(OnboardingStatus.Draft, WorkflowActions.Submit, OnboardingStatus.SentForApproval, AppPermissions.CanCreateOnboarding),
        new(OnboardingStatus.RevisionRequired, WorkflowActions.Resubmit, OnboardingStatus.SentForApproval, AppPermissions.CanCreateOnboarding),
        new(OnboardingStatus.SentForApproval, WorkflowActions.Approve, OnboardingStatus.Approved, AppPermissions.CanApproveOnboarding),
        new(OnboardingStatus.SentForApproval, WorkflowActions.RequestRevision, OnboardingStatus.RevisionRequired, AppPermissions.CanApproveOnboarding),
        new(OnboardingStatus.SentForApproval, WorkflowActions.Reject, OnboardingStatus.Rejected, AppPermissions.CanApproveOnboarding),
    ];

    public static bool CanExecuteTransition(
        string currentState,
        string action,
        ClaimsPrincipal user,
        out string? targetState,
        out string? validationError)
    {
        targetState = null;
        validationError = null;

        var rule = TransitionRules.FirstOrDefault(r =>
            r.CurrentState.Equals(currentState, StringComparison.OrdinalIgnoreCase) &&
            r.Action.Equals(action, StringComparison.OrdinalIgnoreCase));

        if (rule == null)
        {
            validationError = $"Action '{action}' is not valid for status '{currentState}'.";
            return false;
        }

        var hasPermission = user.HasClaim(CustomClaimTypes.Permission, rule.RequiredClaim);
        if (!hasPermission)
        {
            validationError = $"User does not possess permission '{rule.RequiredClaim}' to perform '{action}'.";
            return false;
        }

        targetState = rule.TargetState;
        return true;
    }

    public static bool CanDelete(string currentState, ClaimsPrincipal user, out string? error)
    {
        error = null;
        if (currentState == OnboardingStatus.Approved)
        {
            error = "Approved requests cannot be deleted. The record is permanently locked.";
            return false;
        }

        if (!user.HasClaim(CustomClaimTypes.Permission, AppPermissions.CanDeleteUnapprovedRequest))
        {
            error = $"User requires '{AppPermissions.CanDeleteUnapprovedRequest}' permission to delete unapproved requests.";
            return false;
        }

        return true;
    }

    public static bool CanOverridePayroll(string currentState, ClaimsPrincipal user, out string? error)
    {
        error = null;
        if (currentState == OnboardingStatus.Approved || currentState == OnboardingStatus.Rejected)
        {
            error = $"Cannot override payroll when request is '{currentState}'.";
            return false;
        }

        if (!user.HasClaim(CustomClaimTypes.Permission, AppPermissions.CanOverridePayroll))
        {
            error = $"User requires '{AppPermissions.CanOverridePayroll}' permission to override payroll.";
            return false;
        }

        return true;
    }
}
