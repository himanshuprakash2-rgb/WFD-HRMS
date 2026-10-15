namespace WfdHrms.Data.Entities;

public class PayrollFirm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BranchPayrollMapping> BranchMappings { get; set; } = new List<BranchPayrollMapping>();
    public ICollection<OnboardingRequest> OnboardingRequests { get; set; } = new List<OnboardingRequest>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
