namespace WfdHrms.Data.Entities;

public class UserBranchAssignment
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
