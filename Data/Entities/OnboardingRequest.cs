namespace WfdHrms.Data.Entities;

public static class OnboardingStatus
{
    public const string Draft = "DRAFT";
    public const string SentForApproval = "SENT_FOR_APPROVAL";
    public const string RevisionRequired = "REVISION_REQUIRED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

public class OnboardingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int SelectedPayrollFirmId { get; set; }
    public PayrollFirm SelectedPayrollFirm { get; set; } = null!;

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }

    public string Status { get; set; } = OnboardingStatus.Draft;

    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Designation { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string AadhaarNumber { get; set; } = string.Empty;
    public string PanNumber { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIfscCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerRemarks { get; set; }

    public ICollection<OnboardingAttachment> Attachments { get; set; } = new List<OnboardingAttachment>();
}
