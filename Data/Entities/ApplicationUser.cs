using Microsoft.AspNetCore.Identity;

namespace WfdHrms.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserBranchAssignment> BranchAssignments { get; set; } = new List<UserBranchAssignment>();
    public ICollection<OnboardingRequest> CreatedRequests { get; set; } = new List<OnboardingRequest>();
    public ICollection<OnboardingRequest> ReviewedRequests { get; set; } = new List<OnboardingRequest>();
}
