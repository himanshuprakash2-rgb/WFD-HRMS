namespace WfdHrms.Components.Pages.Onboarding.Models;

public class OnboardingFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public int? BranchId { get; set; }
    public int? PayrollFirmId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
