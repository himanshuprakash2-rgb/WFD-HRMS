using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WfdHrms.Components.Pages.Onboarding.Models;
using WfdHrms.Data;
using WfdHrms.Data.Entities;
using WfdHrms.Services.Identity;
using WfdHrms.Services.StateMachine;

namespace WfdHrms.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IEmployeeCodeGeneratorService _codeGenerator;
    private readonly IGoogleFileUploadService _fileUploadService;
    private readonly NotificationEventBus _notificationBus;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        IDbContextFactory<AppDbContext> contextFactory,
        IEmployeeCodeGeneratorService codeGenerator,
        IGoogleFileUploadService fileUploadService,
        NotificationEventBus notificationBus,
        ILogger<OnboardingService> logger)
    {
        _contextFactory = contextFactory;
        _codeGenerator = codeGenerator;
        _fileUploadService = fileUploadService;
        _notificationBus = notificationBus;
        _logger = logger;
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private static bool IsWorkforceUser(ClaimsPrincipal user) =>
        user.HasClaim(CustomClaimTypes.Permission, AppPermissions.CanApproveOnboarding) ||
        user.HasClaim(CustomClaimTypes.Permission, AppPermissions.CanOverridePayroll);

    public async Task<List<OnboardingRequestVm>> GetRequestsAsync(ClaimsPrincipal user, OnboardingFilterDto filter)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.OnboardingRequests
            .Include(r => r.Branch)
            .Include(r => r.SelectedPayrollFirm)
            .Include(r => r.CreatedByUser)
            .Include(r => r.Attachments)
            .AsNoTracking();

        // Branch scoping: Supervisors only see requests for branches mapped to them in UserBranchAssignment
        if (!IsWorkforceUser(user))
        {
            var userId = GetUserId(user);
            var allowedBranchIds = await context.UserBranchAssignments
                .Where(u => u.UserId == userId)
                .Select(u => u.BranchId)
                .ToListAsync();

            query = query.Where(r => allowedBranchIds.Contains(r.BranchId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "ALL")
        {
            query = query.Where(r => r.Status == filter.Status);
        }

        if (filter.BranchId.HasValue && filter.BranchId > 0)
        {
            query = query.Where(r => r.BranchId == filter.BranchId.Value);
        }

        if (filter.PayrollFirmId.HasValue && filter.PayrollFirmId > 0)
        {
            query = query.Where(r => r.SelectedPayrollFirmId == filter.PayrollFirmId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim();
            query = query.Where(r =>
                r.FullName.Contains(search) ||
                r.AadhaarNumber.Contains(search) ||
                r.PanNumber.Contains(search) ||
                r.MobileNumber.Contains(search));
        }

        var list = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return list.Select(MapToVm).ToList();
    }

    public async Task<OnboardingRequestVm?> GetRequestByIdAsync(Guid id, ClaimsPrincipal user)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.OnboardingRequests
            .Include(r => r.Branch)
            .Include(r => r.SelectedPayrollFirm)
            .Include(r => r.CreatedByUser)
            .Include(r => r.Attachments)
            .AsNoTracking();

        if (!IsWorkforceUser(user))
        {
            var userId = GetUserId(user);
            var allowedBranchIds = await context.UserBranchAssignments
                .Where(u => u.UserId == userId)
                .Select(u => u.BranchId)
                .ToListAsync();

            query = query.Where(r => allowedBranchIds.Contains(r.BranchId));
        }

        var request = await query.FirstOrDefaultAsync(r => r.Id == id);
        return request == null ? null : MapToVm(request);
    }

    public async Task<(bool Success, string? ErrorMessage)> ValidateDuplicateAndFraudAsync(
        string aadhaarNumber,
        string panNumber,
        Guid? currentRequestId = null)
    {
        aadhaarNumber = aadhaarNumber?.Trim() ?? string.Empty;
        panNumber = panNumber?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrEmpty(aadhaarNumber) || string.IsNullOrEmpty(panNumber))
            return (true, null);

        await using var context = await _contextFactory.CreateDbContextAsync();

        // 1. Real-Time check across active OnboardingRequests (including DRAFT, SENT_FOR_APPROVAL, REVISION_REQUIRED)
        var existingReq = await context.OnboardingRequests
            .Include(r => r.CreatedByUser)
            .Include(r => r.Branch)
            .Where(r => r.Status != OnboardingStatus.Rejected && (currentRequestId == null || r.Id != currentRequestId.Value))
            .Where(r => r.AadhaarNumber == aadhaarNumber || r.PanNumber == panNumber)
            .FirstOrDefaultAsync();

        if (existingReq != null)
        {
            var supervisorName = !string.IsNullOrEmpty(existingReq.CreatedByUser?.FullName)
                ? existingReq.CreatedByUser.FullName
                : existingReq.CreatedByUser?.UserName ?? "Another Supervisor";

            var branchName = existingReq.Branch?.Name ?? "another branch";

            // 1:1 Aadhaar–PAN Linkage Fraud Detection
            if (existingReq.AadhaarNumber == aadhaarNumber && !existingReq.PanNumber.Equals(panNumber, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Potential Identity Mismatch: Provided identity credentials conflict with existing records. Please verify physical documents.");
            }
            if (existingReq.PanNumber.Equals(panNumber, StringComparison.OrdinalIgnoreCase) && existingReq.AadhaarNumber != aadhaarNumber)
            {
                return (false, "Potential Identity Mismatch: Provided identity credentials conflict with existing records. Please verify physical documents.");
            }

            return (false, $"Identity already registered in a request created by {supervisorName} at Branch {branchName}.");
        }

        // 2. Real-Time check across Employees
        var existingEmp = await context.Employees
            .Include(e => e.Branch)
            .Where(e => e.AadhaarNumber == aadhaarNumber || e.PanNumber == panNumber)
            .FirstOrDefaultAsync();

        if (existingEmp != null)
        {
            // 1:1 Aadhaar–PAN Linkage Fraud Detection against Employee records
            if (existingEmp.AadhaarNumber == aadhaarNumber && !existingEmp.PanNumber.Equals(panNumber, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Potential Identity Mismatch: Provided identity credentials conflict with existing records. Please verify physical documents.");
            }
            if (existingEmp.PanNumber.Equals(panNumber, StringComparison.OrdinalIgnoreCase) && existingEmp.AadhaarNumber != aadhaarNumber)
            {
                return (false, "Potential Identity Mismatch: Provided identity credentials conflict with existing records. Please verify physical documents.");
            }

            var branchName = existingEmp.Branch?.Name ?? "another branch";
            return (false, $"Identity already registered to existing Employee ({existingEmp.EmployeeCode}) at Branch {branchName}.");
        }

        return (true, null);
    }

    public async Task<(bool Success, Guid RequestId, string? ErrorMessage)> SaveDraftAsync(
        OnboardingRequestVm model,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (string.IsNullOrEmpty(userId))
            return (false, Guid.Empty, "User is not authenticated.");

        // Deduplication & Fraud check
        var val = await ValidateDuplicateAndFraudAsync(model.AadhaarNumber, model.PanNumber, model.Id);
        if (!val.Success)
            return (false, model.Id, val.ErrorMessage);

        await using var context = await _contextFactory.CreateDbContextAsync();

        try
        {
            var isNew = false;
            var request = await context.OnboardingRequests.FirstOrDefaultAsync(r => r.Id == model.Id);

            if (request == null)
            {
                isNew = true;
                request = new OnboardingRequest
                {
                    Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Status = OnboardingStatus.Draft
                };
                context.OnboardingRequests.Add(request);
            }
            else
            {
                if (request.Status == OnboardingStatus.Approved)
                    return (false, request.Id, "Cannot edit an approved request.");
            }

            request.BranchId = model.BranchId;
            request.SelectedPayrollFirmId = model.SelectedPayrollFirmId;
            request.FullName = model.FullName.Trim();
            request.DateOfBirth = model.DateOfBirth ?? DateTime.UtcNow;
            request.Designation = model.Designation.Trim();
            request.MobileNumber = model.MobileNumber.Trim();
            request.AadhaarNumber = model.AadhaarNumber.Trim();
            request.PanNumber = model.PanNumber.Trim().ToUpperInvariant();
            request.BankAccountNumber = model.BankAccountNumber.Trim();
            request.BankIfscCode = model.BankIfscCode.Trim().ToUpperInvariant();
            request.UpdatedAt = DateTime.UtcNow;

            // Synchronous Audit Log in exact same SaveChangesAsync transaction
            context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = isNew ? "CREATE_DRAFT" : "EDIT_DRAFT",
                RecordId = request.Id,
                Details = $"Draft saved for candidate {request.FullName} (Branch: {request.BranchId})",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return (true, request.Id, null);
        }
        catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
        {
            _logger.LogWarning(dbEx, "Unique constraint violation caught on save draft");
            return (false, model.Id, "Conflict detected: This identity record was already submitted in another concurrent request. Please refresh.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft");
            return (false, model.Id, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> SubmitRequestAsync(
        Guid requestId,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.OnboardingRequests
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return (false, "Request not found.");

        var action = request.Status == OnboardingStatus.RevisionRequired
            ? WorkflowActions.Resubmit
            : WorkflowActions.Submit;

        if (!OnboardingWorkflowEngine.CanExecuteTransition(request.Status, action, user, out var targetState, out var valError))
        {
            return (false, valError);
        }

        // Verify mandatory attachments (AADHAAR_CARD, PAN_CARD, BANK_PASSBOOK)
        var hasAadhaar = request.Attachments.Any(a => a.DocumentType == DocumentTypes.AadhaarCard);
        var hasPan = request.Attachments.Any(a => a.DocumentType == DocumentTypes.PanCard);
        var hasBank = request.Attachments.Any(a => a.DocumentType == DocumentTypes.BankPassbook);

        if (!hasAadhaar || !hasPan || !hasBank)
        {
            return (false, "All 3 mandatory documents (Aadhaar Card, PAN Card, and Bank Passbook) are required before submitting for approval.");
        }

        // Deduplication & Fraud check
        var val = await ValidateDuplicateAndFraudAsync(request.AadhaarNumber, request.PanNumber, request.Id);
        if (!val.Success)
            return (false, val.ErrorMessage);

        try
        {
            request.Status = targetState!;
            request.UpdatedAt = DateTime.UtcNow;

            // Synchronous Audit Log
            context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                RecordId = request.Id,
                Details = $"Request transitioned to {targetState} by user {userId}",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
        {
            _logger.LogWarning(dbEx, "Unique constraint violation on submit request");
            return (false, "Conflict detected: This identity record was already submitted in another concurrent request. Please refresh.");
        }
    }

    public async Task<(bool Success, string? EmployeeCode, string? ErrorMessage)> ApproveRequestAsync(
        Guid requestId,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.OnboardingRequests
            .Include(r => r.Branch)
            .Include(r => r.SelectedPayrollFirm)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return (false, null, "Request not found.");

        if (!OnboardingWorkflowEngine.CanExecuteTransition(request.Status, WorkflowActions.Approve, user, out var targetState, out var valError))
        {
            return (false, null, valError);
        }

        try
        {
            // Atomic employee code generation
            var employeeCode = await _codeGenerator.GenerateEmployeeCodeAsync(context, request.BranchId);

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = employeeCode,
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Designation = request.Designation,
                MobileNumber = request.MobileNumber,
                AadhaarNumber = request.AadhaarNumber,
                PanNumber = request.PanNumber,
                BankAccountNumber = request.BankAccountNumber,
                BankIfscCode = request.BankIfscCode,
                BranchId = request.BranchId,
                PayrollFirmId = request.SelectedPayrollFirmId,
                OnboardingRequestId = request.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Employees.Add(employee);

            request.Status = targetState!;
            request.ReviewedByUserId = userId;
            request.ReviewedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            // In-app notification for the creator
            var notification = new InAppNotification
            {
                RecipientUserId = request.CreatedByUserId,
                Title = "Onboarding Request Approved",
                Message = $"Onboarding for {request.FullName} has been approved. Assigned Employee Code: {employeeCode}",
                ReferenceId = request.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            context.InAppNotifications.Add(notification);

            // Synchronous Audit Log
            context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = WorkflowActions.Approve,
                RecordId = request.Id,
                Details = $"Request approved by {userId}. Employee Code generated: {employeeCode}",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            // Publish in-memory event bus notification to trigger NavMenu badge update in real-time
            await _notificationBus.PublishAsync(request.CreatedByUserId, notification);

            return (true, employeeCode, null);
        }
        catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
        {
            _logger.LogWarning(dbEx, "Unique constraint violation on approve");
            return (false, null, "Database conflict: An employee or request with this identity already exists.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Approval error");
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RequestRevisionAsync(
        Guid requestId,
        string remarks,
        ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            return (false, "Reviewer remarks are mandatory when requesting revision.");

        var userId = GetUserId(user);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.OnboardingRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null)
            return (false, "Request not found.");

        if (!OnboardingWorkflowEngine.CanExecuteTransition(request.Status, WorkflowActions.RequestRevision, user, out var targetState, out var valError))
        {
            return (false, valError);
        }

        request.Status = targetState!;
        request.ReviewedByUserId = userId;
        request.ReviewerRemarks = remarks.Trim();
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        var notification = new InAppNotification
        {
            RecipientUserId = request.CreatedByUserId,
            Title = "Revision Requested for Onboarding",
            Message = $"Revision required for {request.FullName}: {remarks.Trim()}",
            ReferenceId = request.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        context.InAppNotifications.Add(notification);

        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = WorkflowActions.RequestRevision,
            RecordId = request.Id,
            Details = $"Revision requested with remarks: {remarks.Trim()}",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        await _notificationBus.PublishAsync(request.CreatedByUserId, notification);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> RejectRequestAsync(
        Guid requestId,
        string remarks,
        ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            return (false, "Reviewer remarks are mandatory when rejecting a request.");

        var userId = GetUserId(user);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.OnboardingRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null)
            return (false, "Request not found.");

        if (!OnboardingWorkflowEngine.CanExecuteTransition(request.Status, WorkflowActions.Reject, user, out var targetState, out var valError))
        {
            return (false, valError);
        }

        request.Status = targetState!;
        request.ReviewedByUserId = userId;
        request.ReviewerRemarks = remarks.Trim();
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        var notification = new InAppNotification
        {
            RecipientUserId = request.CreatedByUserId,
            Title = "Onboarding Request Rejected",
            Message = $"Request for {request.FullName} was rejected. Reason: {remarks.Trim()}",
            ReferenceId = request.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        context.InAppNotifications.Add(notification);

        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = WorkflowActions.Reject,
            RecordId = request.Id,
            Details = $"Request rejected with remarks: {remarks.Trim()}",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        await _notificationBus.PublishAsync(request.CreatedByUserId, notification);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> OverridePayrollAndEditAsync(
        Guid requestId,
        int newPayrollFirmId,
        OnboardingRequestVm? editedFields,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.OnboardingRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null)
            return (false, "Request not found.");

        if (!OnboardingWorkflowEngine.CanOverridePayroll(request.Status, user, out var error))
        {
            return (false, error);
        }

        // Validate that new payroll firm is mapped to this request's branch via explicit LINQ
        var isMapped = await context.BranchPayrollMappings
            .AnyAsync(m => m.BranchId == request.BranchId && m.PayrollFirmId == newPayrollFirmId && m.IsActive);

        if (!isMapped)
        {
            return (false, "Selected payroll firm is not actively mapped to this request's branch.");
        }

        var oldPayrollId = request.SelectedPayrollFirmId;
        request.SelectedPayrollFirmId = newPayrollFirmId;

        if (editedFields != null)
        {
            request.FullName = editedFields.FullName.Trim();
            request.Designation = editedFields.Designation.Trim();
            request.MobileNumber = editedFields.MobileNumber.Trim();
            request.BankAccountNumber = editedFields.BankAccountNumber.Trim();
            request.BankIfscCode = editedFields.BankIfscCode.Trim().ToUpperInvariant();
        }

        request.UpdatedAt = DateTime.UtcNow;

        var details = $"Payroll Firm changed from {oldPayrollId} to {newPayrollFirmId} by workforce user {userId}.";

        var notification = new InAppNotification
        {
            RecipientUserId = request.CreatedByUserId,
            Title = "Payroll Firm Modified by Workforce",
            Message = $"The assigned payroll firm for candidate {request.FullName} was adjusted during review.",
            ReferenceId = request.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        context.InAppNotifications.Add(notification);

        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = WorkflowActions.OverridePayroll,
            RecordId = request.Id,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        await _notificationBus.PublishAsync(request.CreatedByUserId, notification);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteRequestAsync(
        Guid requestId,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.OnboardingRequests
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return (false, "Request not found.");

        if (!OnboardingWorkflowEngine.CanDelete(request.Status, user, out var error))
        {
            return (false, error);
        }

        // Cleanup attachment files
        foreach (var att in request.Attachments)
        {
            await _fileUploadService.DeleteFileAsync(att.GoogleDriveFileId);
        }

        // Synchronous Audit Log
        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = WorkflowActions.Delete,
            RecordId = request.Id,
            Details = $"Request for {request.FullName} deleted by user {userId}",
            CreatedAt = DateTime.UtcNow
        });

        context.OnboardingRequests.Remove(request);
        await context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> SaveAttachmentAsync(
        Guid requestId,
        string documentType,
        string fileId,
        string fileUrl,
        long fileSizeBytes,
        ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.OnboardingRequests
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null)
            return (false, "Request not found.");

        if (request.Status == OnboardingStatus.Approved)
            return (false, "Cannot modify attachments on an approved request.");

        // If replacing an existing file during REVISION_REQUIRED or DRAFT, delete the old file reference
        var existing = request.Attachments.FirstOrDefault(a => a.DocumentType == documentType);
        if (existing != null)
        {
            await _fileUploadService.DeleteFileAsync(existing.GoogleDriveFileId);
            context.OnboardingAttachments.Remove(existing);
        }

        var attachment = new OnboardingAttachment
        {
            Id = Guid.NewGuid(),
            OnboardingRequestId = requestId,
            DocumentType = documentType,
            GoogleDriveFileId = fileId,
            FileUrl = fileUrl,
            MimeType = "image/webp",
            FileSizeBytes = fileSizeBytes,
            CreatedAt = DateTime.UtcNow
        };
        context.OnboardingAttachments.Add(attachment);

        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "ATTACHMENT_SAVED",
            RecordId = requestId,
            Details = $"Attachment uploaded: {documentType} ({fileSizeBytes} bytes)",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(Guid requestId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .Where(a => a.RecordId == requestId)
            .OrderByDescending(a => a.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<InAppNotification>> GetUserNotificationsAsync(string userId, int maxCount = 20)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InAppNotifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(maxCount)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetUnreadNotificationCountAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InAppNotifications
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
    }

    public async Task MarkNotificationAsReadAsync(long notificationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.InAppNotifications.FirstOrDefaultAsync(n => n.Id == notificationId);
        if (item != null)
        {
            item.IsRead = true;
            await context.SaveChangesAsync();
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627;
        }
        return false;
    }

    private static OnboardingRequestVm MapToVm(OnboardingRequest r)
    {
        return new OnboardingRequestVm
        {
            Id = r.Id,
            BranchId = r.BranchId,
            BranchName = r.Branch?.Name ?? "",
            SelectedPayrollFirmId = r.SelectedPayrollFirmId,
            PayrollFirmName = r.SelectedPayrollFirm?.Name ?? "",
            FullName = r.FullName,
            DateOfBirth = r.DateOfBirth,
            Designation = r.Designation,
            MobileNumber = r.MobileNumber,
            AadhaarNumber = r.AadhaarNumber,
            PanNumber = r.PanNumber,
            BankAccountNumber = r.BankAccountNumber,
            BankIfscCode = r.BankIfscCode,
            Status = r.Status,
            ReviewerRemarks = r.ReviewerRemarks,
            CreatedByUserId = r.CreatedByUserId,
            CreatedByUserName = r.CreatedByUser?.FullName ?? r.CreatedByUser?.UserName ?? "",
            Attachments = r.Attachments.Select(a => new AttachmentVm
            {
                Id = a.Id,
                DocumentType = a.DocumentType,
                FileId = a.GoogleDriveFileId,
                FileUrl = a.FileUrl,
                FileSizeBytes = a.FileSizeBytes
            }).ToList()
        };
    }
}
