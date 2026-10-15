using Microsoft.EntityFrameworkCore;
using WfdHrms.Data;
using WfdHrms.Data.Entities;

namespace WfdHrms.Services;

public class MasterDataService : IMasterDataService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<MasterDataService> _logger;

    public MasterDataService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<MasterDataService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // --- Branches ---
    public async Task<List<Branch>> GetAllBranchesAsync(bool activeOnly = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Branches.AsNoTracking();
        if (activeOnly)
            query = query.Where(b => b.IsActive);
        return await query.OrderBy(b => b.Name).ToListAsync();
    }

    public async Task<Branch?> GetBranchByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<(bool Success, string? Error)> SaveBranchAsync(Branch branch)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var codeExists = await context.Branches
                .AnyAsync(b => b.Code == branch.Code && b.Id != branch.Id);

            if (codeExists)
                return (false, $"A branch with code '{branch.Code}' already exists.");

            if (branch.Id == 0)
            {
                branch.CreatedAt = DateTime.UtcNow;
                context.Branches.Add(branch);
            }
            else
            {
                var existing = await context.Branches.FirstOrDefaultAsync(b => b.Id == branch.Id);
                if (existing == null) return (false, "Branch not found.");
                existing.Name = branch.Name;
                existing.Code = branch.Code;
                existing.IsActive = branch.IsActive;
            }

            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save branch");
            return (false, ex.Message);
        }
    }

    public async Task<bool> ToggleBranchActiveAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var branch = await context.Branches.FirstOrDefaultAsync(b => b.Id == id);
        if (branch == null) return false;
        branch.IsActive = !branch.IsActive;
        await context.SaveChangesAsync();
        return true;
    }

    // --- Payroll Firms ---
    public async Task<List<PayrollFirm>> GetAllPayrollFirmsAsync(bool activeOnly = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.PayrollFirms.AsNoTracking();
        if (activeOnly)
            query = query.Where(p => p.IsActive);
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<PayrollFirm?> GetPayrollFirmByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PayrollFirms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(bool Success, string? Error)> SavePayrollFirmAsync(PayrollFirm firm)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var codeExists = await context.PayrollFirms
                .AnyAsync(p => p.Code == firm.Code && p.Id != firm.Id);

            if (codeExists)
                return (false, $"A payroll firm with code '{firm.Code}' already exists.");

            if (firm.Id == 0)
            {
                firm.CreatedAt = DateTime.UtcNow;
                context.PayrollFirms.Add(firm);
            }
            else
            {
                var existing = await context.PayrollFirms.FirstOrDefaultAsync(p => p.Id == firm.Id);
                if (existing == null) return (false, "Payroll firm not found.");
                existing.Name = firm.Name;
                existing.Code = firm.Code;
                existing.IsActive = firm.IsActive;
            }

            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save payroll firm");
            return (false, ex.Message);
        }
    }

    public async Task<bool> TogglePayrollFirmActiveAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var firm = await context.PayrollFirms.FirstOrDefaultAsync(p => p.Id == id);
        if (firm == null) return false;
        firm.IsActive = !firm.IsActive;
        await context.SaveChangesAsync();
        return true;
    }

    // --- Branch Payroll Mappings ---
    public async Task<List<BranchPayrollMapping>> GetMappingsForBranchAsync(int branchId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BranchPayrollMappings
            .Include(m => m.PayrollFirm)
            .Where(m => m.BranchId == branchId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<PayrollFirm>> GetValidPayrollFirmsForBranchAsync(int branchId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Explicit LINQ query for branch-payroll scoping per specification
        return await (from m in context.BranchPayrollMappings
                      join p in context.PayrollFirms on m.PayrollFirmId equals p.Id
                      where m.BranchId == branchId && m.IsActive && p.IsActive
                      orderby p.Name
                      select p)
                      .AsNoTracking()
                      .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateBranchPayrollMappingsAsync(int branchId, List<int> selectedPayrollFirmIds)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.BranchPayrollMappings
                .Where(m => m.BranchId == branchId)
                .ToListAsync();

            context.BranchPayrollMappings.RemoveRange(existing);

            foreach (var payrollFirmId in selectedPayrollFirmIds)
            {
                context.BranchPayrollMappings.Add(new BranchPayrollMapping
                {
                    BranchId = branchId,
                    PayrollFirmId = payrollFirmId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update branch payroll mappings for branch {BranchId}", branchId);
            return (false, ex.Message);
        }
    }

    // --- User Branch Assignments ---
    public async Task<List<int>> GetAssignedBranchIdsForUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserBranchAssignments
            .Where(u => u.UserId == userId)
            .Select(u => u.BranchId)
            .ToListAsync();
    }

    public async Task<List<Branch>> GetAssignedBranchesForUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await (from u in context.UserBranchAssignments
                      join b in context.Branches on u.BranchId equals b.Id
                      where u.UserId == userId && b.IsActive
                      orderby b.Name
                      select b)
                      .AsNoTracking()
                      .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateUserBranchAssignmentsAsync(string userId, List<int> branchIds)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.UserBranchAssignments
                .Where(u => u.UserId == userId)
                .ToListAsync();

            context.UserBranchAssignments.RemoveRange(existing);

            foreach (var branchId in branchIds)
            {
                context.UserBranchAssignments.Add(new UserBranchAssignment
                {
                    UserId = userId,
                    BranchId = branchId,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user branch assignments for user {UserId}", userId);
            return (false, ex.Message);
        }
    }
}
