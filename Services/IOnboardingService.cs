using System.Security.Claims;
using WfdHrms.Components.Pages.Onboarding.Models;
using WfdHrms.Data.Entities;

namespace WfdHrms.Services;

public interface IOnboardingService
{
    Task<List<OnboardingRequestVm>> GetRequestsAsync(ClaimsPrincipal user, OnboardingFilterDto filter);
    Task<OnboardingRequestVm?> GetRequestByIdAsync(Guid id, ClaimsPrincipal user);

    Task<(bool Success, string? ErrorMessage)> ValidateDuplicateAndFraudAsync(
        string aadhaarNumber,
        string panNumber,
        Guid? currentRequestId = null);

    Task<(bool Success, Guid RequestId, string? ErrorMessage)> SaveDraftAsync(
        OnboardingRequestVm model,
        ClaimsPrincipal user);

    Task<(bool Success, string? ErrorMessage)> SubmitRequestAsync(
        Guid requestId,
        ClaimsPrincipal user);

    Task<(bool Success, string? EmployeeCode, string? ErrorMessage)> ApproveRequestAsync(
        Guid requestId,
        ClaimsPrincipal user);

    Task<(bool Success, string? ErrorMessage)> RequestRevisionAsync(
        Guid requestId,
        string remarks,
        ClaimsPrincipal user);

    Task<(bool Success, string? ErrorMessage)> RejectRequestAsync(
        Guid requestId,
        string remarks,
        ClaimsPrincipal user);

    Task<(bool Success, string? ErrorMessage)> OverridePayrollAndEditAsync(
        Guid requestId,
        int newPayrollFirmId,
        OnboardingRequestVm? editedFields,
        ClaimsPrincipal user);

    Task<(bool Success, string? ErrorMessage)> DeleteRequestAsync(
        Guid requestId,
        ClaimsPrincipal user);

    Task<(bool Success, string? ErrorMessage)> SaveAttachmentAsync(
        Guid requestId,
        string documentType,
        string fileId,
        string fileUrl,
        long fileSizeBytes,
        ClaimsPrincipal user);

    Task<List<AuditLog>> GetAuditLogsAsync(Guid requestId);
    Task<List<InAppNotification>> GetUserNotificationsAsync(string userId, int maxCount = 20);
    Task<int> GetUnreadNotificationCountAsync(string userId);
    Task MarkNotificationAsReadAsync(long notificationId);
}
