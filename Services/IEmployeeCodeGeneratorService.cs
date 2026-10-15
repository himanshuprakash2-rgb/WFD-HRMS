using WfdHrms.Data;

namespace WfdHrms.Services;

public interface IEmployeeCodeGeneratorService
{
    Task<string> GenerateEmployeeCodeAsync(AppDbContext context, int branchId, CancellationToken cancellationToken = default);
}
