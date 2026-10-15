using System.ComponentModel.DataAnnotations;
using WfdHrms.Data.Entities;

namespace WfdHrms.Components.Pages.Onboarding.Models;

public class OnboardingRequestVm
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Please select a branch")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Please select a payroll firm")]
    public int SelectedPayrollFirmId { get; set; }

    [Required(ErrorMessage = "Full Name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full Name must be between 2 and 150 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of Birth is required")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Designation is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Designation must be between 2 and 100 characters")]
    public string Designation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile Number is required")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aadhaar Number is required")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar must be a 12-digit number")]
    public string AadhaarNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "PAN Number is required")]
    [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Enter a valid 10-character PAN (e.g. ABCDE1234F)")]
    public string PanNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank Account Number is required")]
    [RegularExpression(@"^\d{9,18}$", ErrorMessage = "Bank Account number must be between 9 and 18 digits")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank IFSC Code is required")]
    [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Enter a valid 11-character IFSC Code (e.g. SBIN0001234)")]
    public string BankIfscCode { get; set; } = string.Empty;

    public string Status { get; set; } = OnboardingStatus.Draft;
    public string? ReviewerRemarks { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string PayrollFirmName { get; set; } = string.Empty;

    public List<AttachmentVm> Attachments { get; set; } = new();
}

public class AttachmentVm
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
