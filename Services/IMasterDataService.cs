using WfdHrms.Data.Entities;

namespace WfdHrms.Services;

public interface IMasterDataService
{
    // Branches
    Task<List<Branch>> GetAllBranchesAsync(bool activeOnly = false);
    Task<Branch?> GetBranchByIdAsync(int id);
    Task<(bool Success, string? Error)> SaveBranchAsync(Branch branch);
    Task<bool> ToggleBranchActiveAsync(int id);

    // Payroll Firms
    Task<List<PayrollFirm>> GetAllPayrollFirmsAsync(bool activeOnly = false);
    Task<PayrollFirm?> GetPayrollFirmByIdAsync(int id);
    Task<(bool Success, string? Error)> SavePayrollFirmAsync(PayrollFirm firm);
    Task<bool> TogglePayrollFirmActiveAsync(int id);

    // Branch Payroll Mappings
    Task<List<BranchPayrollMapping>> GetMappingsForBranchAsync(int branchId);
    Task<List<PayrollFirm>> GetValidPayrollFirmsForBranchAsync(int branchId);
    Task<(bool Success, string? Error)> UpdateBranchPayrollMappingsAsync(int branchId, List<int> selectedPayrollFirmIds);

    // User Branch Assignments
    Task<List<int>> GetAssignedBranchIdsForUserAsync(string userId);
    Task<List<Branch>> GetAssignedBranchesForUserAsync(string userId);
    Task<(bool Success, string? Error)> UpdateUserBranchAssignmentsAsync(string userId, List<int> branchIds);
}
