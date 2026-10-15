using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WfdHrms.Data.Entities;

namespace WfdHrms.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<PayrollFirm> PayrollFirms => Set<PayrollFirm>();
    public DbSet<BranchPayrollMapping> BranchPayrollMappings => Set<BranchPayrollMapping>();
    public DbSet<UserBranchAssignment> UserBranchAssignments => Set<UserBranchAssignment>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OnboardingRequest> OnboardingRequests => Set<OnboardingRequest>();
    public DbSet<OnboardingAttachment> OnboardingAttachments => Set<OnboardingAttachment>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeCodeConfig> EmployeeCodeConfigs => Set<EmployeeCodeConfig>();
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Branch
        builder.Entity<Branch>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Code).HasMaxLength(20).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
        });

        // PayrollFirm
        builder.Entity<PayrollFirm>(p =>
        {
            p.HasKey(x => x.Id);
            p.Property(x => x.Name).HasMaxLength(150).IsRequired();
            p.Property(x => x.Code).HasMaxLength(20).IsRequired();
            p.HasIndex(x => x.Code).IsUnique();
        });

        // BranchPayrollMapping (Composite Key)
        builder.Entity<BranchPayrollMapping>(m =>
        {
            m.HasKey(x => new { x.BranchId, x.PayrollFirmId });
            m.HasOne(x => x.Branch)
             .WithMany(x => x.PayrollMappings)
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            m.HasOne(x => x.PayrollFirm)
             .WithMany(x => x.BranchMappings)
             .HasForeignKey(x => x.PayrollFirmId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // UserBranchAssignment (Composite Key)
        builder.Entity<UserBranchAssignment>(u =>
        {
            u.HasKey(x => new { x.UserId, x.BranchId });
            u.HasOne(x => x.User)
             .WithMany(x => x.BranchAssignments)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            u.HasOne(x => x.Branch)
             .WithMany(x => x.UserAssignments)
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // RolePermission
        builder.Entity<RolePermission>(r =>
        {
            r.HasKey(x => x.Id);
            r.Property(x => x.PermissionName).HasMaxLength(100).IsRequired();
            r.HasIndex(x => new { x.RoleId, x.PermissionName }).IsUnique();
        });

        // OnboardingRequest
        builder.Entity<OnboardingRequest>(r =>
        {
            r.HasKey(x => x.Id);
            r.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            r.Property(x => x.Designation).HasMaxLength(100).IsRequired();
            r.Property(x => x.MobileNumber).HasMaxLength(15).IsRequired();
            r.Property(x => x.AadhaarNumber).HasMaxLength(12).IsRequired();
            r.Property(x => x.PanNumber).HasMaxLength(10).IsRequired();
            r.Property(x => x.BankAccountNumber).HasMaxLength(30).IsRequired();
            r.Property(x => x.BankIfscCode).HasMaxLength(15).IsRequired();
            r.Property(x => x.Status).HasMaxLength(30).IsRequired();

            r.HasOne(x => x.Branch)
             .WithMany(x => x.OnboardingRequests)
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            r.HasOne(x => x.SelectedPayrollFirm)
             .WithMany(x => x.OnboardingRequests)
             .HasForeignKey(x => x.SelectedPayrollFirmId)
             .OnDelete(DeleteBehavior.Restrict);

            r.HasOne(x => x.CreatedByUser)
             .WithMany(x => x.CreatedRequests)
             .HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            r.HasOne(x => x.ReviewedByUser)
             .WithMany(x => x.ReviewedRequests)
             .HasForeignKey(x => x.ReviewedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // Filtered Unique Indexes (locking per specification: enforce unique non-rejected requests)
            r.HasIndex(x => x.AadhaarNumber)
             .IsUnique()
             .HasFilter("[Status] <> 'REJECTED'");

            r.HasIndex(x => x.PanNumber)
             .IsUnique()
             .HasFilter("[Status] <> 'REJECTED'");
        });

        // OnboardingAttachment
        builder.Entity<OnboardingAttachment>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
            a.Property(x => x.GoogleDriveFileId).HasMaxLength(150);
            a.Property(x => x.FileUrl).HasMaxLength(500);
            a.Property(x => x.MimeType).HasMaxLength(50);

            a.HasOne(x => x.OnboardingRequest)
             .WithMany(x => x.Attachments)
             .HasForeignKey(x => x.OnboardingRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Employee
        builder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.EmployeeCode).IsUnique();

            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Designation).HasMaxLength(100).IsRequired();
            e.Property(x => x.MobileNumber).HasMaxLength(15).IsRequired();
            e.Property(x => x.AadhaarNumber).HasMaxLength(12).IsRequired();
            e.HasIndex(x => x.AadhaarNumber).IsUnique();

            e.Property(x => x.PanNumber).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.PanNumber).IsUnique();

            e.Property(x => x.BankAccountNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.BankIfscCode).HasMaxLength(15).IsRequired();

            e.HasOne(x => x.Branch)
             .WithMany(x => x.Employees)
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PayrollFirm)
             .WithMany(x => x.Employees)
             .HasForeignKey(x => x.PayrollFirmId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // EmployeeCodeConfig
        builder.Entity<EmployeeCodeConfig>(c =>
        {
            c.HasKey(x => x.Id);
            c.Property(x => x.TemplatePattern).HasMaxLength(100).IsRequired();
        });

        // InAppNotification
        builder.Entity<InAppNotification>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Title).HasMaxLength(150).IsRequired();
            n.Property(x => x.Message).HasMaxLength(500).IsRequired();
            n.HasOne(x => x.RecipientUser)
             .WithMany()
             .HasForeignKey(x => x.RecipientUserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog
        builder.Entity<AuditLog>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.Action).HasMaxLength(100).IsRequired();
            a.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            a.HasIndex(x => x.RecordId);
        });
    }
}
