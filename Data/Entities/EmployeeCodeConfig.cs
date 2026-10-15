namespace WfdHrms.Data.Entities;

public class EmployeeCodeConfig
{
    public int Id { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string TemplatePattern { get; set; } = "EMP-{BRANCH}-{YEAR}-{SEQ:4}";
    public int CurrentSequence { get; set; } = 0;
    public int LastResetYear { get; set; } = DateTime.UtcNow.Year;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
