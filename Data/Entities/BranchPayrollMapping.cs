namespace WfdHrms.Data.Entities;

public class BranchPayrollMapping
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public int PayrollFirmId { get; set; }
    public PayrollFirm PayrollFirm { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
