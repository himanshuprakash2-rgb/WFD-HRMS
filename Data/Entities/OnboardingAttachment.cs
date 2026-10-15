namespace WfdHrms.Data.Entities;

public static class DocumentTypes
{
    public const string AadhaarCard = "AADHAAR_CARD";
    public const string PanCard = "PAN_CARD";
    public const string BankPassbook = "BANK_PASSBOOK";

    public static readonly string[] All = [AadhaarCard, PanCard, BankPassbook];
}

public class OnboardingAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OnboardingRequestId { get; set; }
    public OnboardingRequest OnboardingRequest { get; set; } = null!;

    public string DocumentType { get; set; } = string.Empty;
    public string GoogleDriveFileId { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/webp";
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
