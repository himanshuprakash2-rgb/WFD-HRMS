namespace WfdHrms.Data.Entities;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Designation { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string AadhaarNumber { get; set; } = string.Empty;
    public string PanNumber { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIfscCode { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int PayrollFirmId { get; set; }
    public PayrollFirm PayrollFirm { get; set; } = null!;

    public Guid? OnboardingRequestId { get; set; }
    public OnboardingRequest? OnboardingRequest { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
