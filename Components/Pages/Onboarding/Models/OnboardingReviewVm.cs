using System.ComponentModel.DataAnnotations;

namespace WfdHrms.Components.Pages.Onboarding.Models;

public class OnboardingReviewVm
{
    public Guid RequestId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public int? NewPayrollFirmId { get; set; }
}
