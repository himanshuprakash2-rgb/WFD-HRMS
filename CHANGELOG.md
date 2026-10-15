# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2026-10-14 17:58] - Native Blazor InputFile Streaming & Local `wwwroot/uploads` Storage
### Added
- **`IFileUploadService` & `LocalFileUploadService`** (`Services/`):
  - Directly streams and saves uploads into `wwwroot/uploads/{document_type}/{unique_id}.{ext}`.
  - Implements safe directory creation with fallback to `Directory.GetCurrentDirectory() / wwwroot`.
  - Also implements `IGoogleFileUploadService` for full backward compatibility.
- **SignalR Hub Capacity Expansion**:
  - Configured `MaximumReceiveMessageSize = 10 * 1024 * 1024` (10 MB) in `Program.cs`.
- **Native Document Support**:
  - Added support for PDF documents along with JPG, PNG, and WebP images.
  - Added PDF document preview cards in `OnboardingEdit.razor`.

### Changed
- **Replaced JavaScript Base64 Upload Pipeline with Blazor Native `<InputFile>`**:
  - Replaced raw `<input type="file">` and `imageCompressor.js` with `<InputFile OnChange="..." />` using `IBrowserFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024)`.
  - Eliminated the 32 KB SignalR Base64 bottleneck that was causing uploads to drop.
  - Removed unused `imageCompressor.js` script reference from `App.razor`.

## [2026-10-14 17:13] - Dedicated Entry Pages & Global Toaster Notification System
### Added
- **Global Toaster Notification System**:
  - Implemented `IToastService` and `ToastService` in `Services/` supporting Success, Error, Info, and Warning toasts with thread-safe queueing and timer-based auto-dismissal (6 seconds for success/info, 8 seconds for errors).
  - Created `Components/Shared/ToastContainer.razor` rendered at top-right (`fixed top-20 right-6 z-50`) in `MainLayout.razor` adhering to UX contrast, larger clickable areas, and clear badge labels.
  - Connected `NotificationEventBus` to `IToastService` in `NavMenu.razor.cs` so background in-app notifications immediately trigger toast alerts for active users.
- **Dedicated Data Entry & Edit Pages**:
  - `Components/Pages/Admin/Branches/BranchEdit.razor` & `.cs`: Dedicated page (`/admin/branches/new` and `/admin/branches/edit/{id}`) with breadcrumbs, uppercase code handling, and toast feedback.
  - `Components/Pages/Admin/PayrollFirms/PayrollFirmEdit.razor` & `.cs`: Dedicated page (`/admin/payroll-firms/new` and `/admin/payroll-firms/edit/{id}`) with breadcrumbs and toast feedback.
  - `Components/Pages/Admin/Rbac/RoleCreate.razor` & `.cs`: Dedicated page (`/admin/roles/new`) for role creation.
  - `Components/Pages/Admin/Rbac/UserRoleEdit.razor` & `.cs`: Dedicated page (`/admin/user-roles/edit/{userId}`) for user role assignment.
  - Added `GetUserByIdAsync` to `IRbacService` and `RbacService`.

### Changed
- **Purged Modal Overlays from Data Entry**:
  - Removed all form overlay backdrops and dialogs from `BranchList.razor`, `PayrollFirmList.razor`, `RoleList.razor`, and `UserRoles.razor`.
  - Replaced modal trigger buttons with direct links (`<a href="...">`) to dedicated pages.
- **Purged Inline HTML Banner Alerts**:
  - Replaced all inline error and success banner alerts across `BranchList`, `PayrollFirmList`, `RoleList`, `UserRoles`, `BranchPayrollMapping`, `UserBranchAssign`, `RolePermissions`, `OnboardingList`, `OnboardingEdit`, and `OnboardingReview` with `ToastService.ShowError(...)` and `ToastService.ShowSuccess(...)`.

## [2026-10-14 16:06] - Standard Conventions, 2-Hour Cookie Expiry & Core Rule Update
### Added
- **Core Operating Rule (Section 1.E in `GEMINI.md`)**: Mandated adhering strictly to Microsoft/.NET default conventions and guidelines, prohibiting custom routes/rules where standard defaults exist, and requiring prior user discussion before deviating.
- **2-Hour Sliding Cookie Lifetime**: Configured `options.ExpireTimeSpan = TimeSpan.FromHours(2)` with sliding expiration in `Program.cs`.

### Changed
- Standardized authentication routes to native framework conventions:
  - Landing and login route updated to `@page "/Account/Login"` (alongside `@page "/"`).
  - Auth Minimal API endpoints standardized to `/Account/Login` and `/Account/Logout`.
  - Sign out form in `NavMenu.razor` and redirection helper in `RedirectToLogin.razor` aligned with `/Account/Login` and `/Account/Logout`.

## [2026-10-14 15:32] - Blazor Server Migration, SWIFT HRMS 70/30 Landing & Strict Authorization
### Added
- **.NET 10 Blazor Server Architecture**: Fully transitioned the application from MVC to Blazor Server with Interactive Server render mode.
- **Modern 70/30 SWIFT HRMS Landing Page**: Central landing page at `/` and `/login` featuring enterprise branding (Centralized Attendance, Leaves Management, Workforce Transfers, Identity Fraud Locking, Dynamic RBAC) on the 70% left section, and a clean professional login card on the 30% right section.
- **Operational Dashboard (`/dashboard`)**: Added role-scoped dashboard displaying Master Data (Branches, Payroll Firms, Matching, User-Branch Assignments) and Dynamic RBAC (Roles, Permissions Matrix, User Roles) cards for administrators, and Onboarding workflow cards for staff.
- **Super Administrator Account**: Seeded account for "Himanshu Prakash" (username: `admin`, email: `admin@wfdhrms.com`) with full `SystemAdmin` permissions.
- **Zero-Mocking Rule**: Added strict rule in `GEMINI.md` permanently prohibiting mock account switchers, testing panels, and unrequested prototype shortcuts.
- **Strict Authorization Redirection**: Added `RedirectToLogin` component inside `Routes.razor` `<NotAuthorized>` ensuring zero unauthenticated access across all internal routes.

### Changed
- Replaced MVC routing and layouts with Blazor components (`App.razor`, `Routes.razor`, `MainLayout.razor`, `NavMenu.razor`).
- Updated `MainLayout.razor` to render full-screen for unauthenticated landing and header/footer for authenticated app sessions.
- Purged all demo branches, demo payroll firms, demo branch-payroll mappings, demo requests, and demo users from SQL Server (`WfdHrmsDb`), leaving a completely clean database.

### Removed
- Removed legacy MVC controllers (`Controllers/HomeController.cs`) and views (`Views/`).
- Removed all quick-switch test account selection buttons from the login interface.

## [2026-10-07 17:24] - UX Guardrails & HRMS Prototype
### Added
- Documented precise demographic (30-60s) UX guardrails in `GEMINI.md` to mandate soft contrast, descriptive labels, and larger touch targets.
- Integrated official brand colors (`#0077cc`, `#fff`, `#1b6ec2`, `#1861ac`) into `tailwind.config.js` via the `brand-` prefix.
- Built a static HRMS Dashboard prototype in `Views/Home/Index.cshtml` featuring Quick Actions and a Leave Summary to validate the new UX principles.

### Changed
- Rebuilt `_Layout.cshtml` header to use the primary brand background (`bg-brand-primary`) and increased touch padding on navigation links.
- Adjusted `_Layout.cshtml` to fix overwhelming header colors by switching to `bg-white` with a brand-colored bottom border, and reduced vertical thickness of both header and footer.

## [2026-10-07 17:09] - Project Initialization & Tailwind Setup
### Added
- Base ASP.NET Core MVC project structure.
- `Services` directory to enforce the thin-controller architecture.
- `GEMINI.md` to persist the strict project rules, architecture guidelines, and technology constraints.
- `Styles/input.css` and `tailwind.config.js` for standalone Tailwind compilation.
- MSBuild target (`BuildTailwind`) in `WfdHrms.csproj` to automatically download and run the Tailwind standalone CLI executable during the build process without Node.js.

### Changed
- Replaced default Bootstrap navigation and layout in `Views/Shared/_Layout.cshtml` with Tailwind utility classes.
- Updated `Views/Home/Index.cshtml` and `Views/Home/Privacy.cshtml` to use Tailwind formatting.

### Removed
- Entirely purged `wwwroot/lib/bootstrap` and all Bootstrap CSS/JS references from the project.
