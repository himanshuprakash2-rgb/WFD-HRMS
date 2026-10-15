using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WfdHrms.Data;
using WfdHrms.Data.Entities;

namespace WfdHrms.Services;

public class EmployeeCodeGeneratorService : IEmployeeCodeGeneratorService
{
    private readonly IConfiguration _configuration;

    public EmployeeCodeGeneratorService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GenerateEmployeeCodeAsync(AppDbContext context, int branchId, CancellationToken cancellationToken = default)
    {
        var branch = await context.Branches.FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken);
        var branchCode = branch?.Code ?? "HQ";

        // Query branch-specific config or default config
        var config = await context.EmployeeCodeConfigs
            .FirstOrDefaultAsync(c => c.BranchId == branchId, cancellationToken)
            ?? await context.EmployeeCodeConfigs
            .FirstOrDefaultAsync(c => c.BranchId == null, cancellationToken);

        var currentYear = DateTime.UtcNow.Year;

        if (config == null)
        {
            var defaultPattern = _configuration.GetValue<string>("EmployeeCodeConfig:DefaultTemplate") ?? "EMP-{BRANCH}-{YEAR}-{SEQ:4}";
            config = new EmployeeCodeConfig
            {
                BranchId = null,
                TemplatePattern = defaultPattern,
                CurrentSequence = 0,
                LastResetYear = currentYear,
                UpdatedAt = DateTime.UtcNow
            };
            context.EmployeeCodeConfigs.Add(config);
        }

        // Year reset check if template includes {YEAR}
        if (config.LastResetYear != currentYear)
        {
            config.CurrentSequence = 0;
            config.LastResetYear = currentYear;
        }

        config.CurrentSequence++;
        config.UpdatedAt = DateTime.UtcNow;

        var pattern = config.TemplatePattern;
        var code = pattern
            .Replace("{BRANCH}", branchCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{YEAR}", currentYear.ToString(), StringComparison.OrdinalIgnoreCase);

        // Replace {SEQ:N}
        var match = Regex.Match(code, @"\{SEQ:(\d+)\}", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var digits))
        {
            var seqStr = config.CurrentSequence.ToString().PadLeft(digits, '0');
            code = code.Replace(match.Value, seqStr);
        }
        else
        {
            code = code.Replace("{SEQ}", config.CurrentSequence.ToString().PadLeft(4, '0'), StringComparison.OrdinalIgnoreCase);
        }

        return code;
    }
}
