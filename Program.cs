using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WfdHrms.Components;
using WfdHrms.Data;
using WfdHrms.Data.Entities;
using WfdHrms.Data.Seeding;
using WfdHrms.Services;
using WfdHrms.Services.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration with IDbContextFactory for Blazor Server Concurrency
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.\\SQLEXPRESS;Database=WfdHrmsDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// 2. ASP.NET Core Identity & Dynamic Claims-Based RBAC
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthentication();
builder.Services.AddCascadingAuthenticationState();

// 3. Authorization Policies for Granular Permissions
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPermissions.CanCreateOnboarding, policy =>
        policy.RequireClaim(CustomClaimTypes.Permission, AppPermissions.CanCreateOnboarding));

    options.AddPolicy(AppPermissions.CanApproveOnboarding, policy =>
        policy.RequireClaim(CustomClaimTypes.Permission, AppPermissions.CanApproveOnboarding));

    options.AddPolicy(AppPermissions.CanOverridePayroll, policy =>
        policy.RequireClaim(CustomClaimTypes.Permission, AppPermissions.CanOverridePayroll));

    options.AddPolicy(AppPermissions.CanDeleteUnapprovedRequest, policy =>
        policy.RequireClaim(CustomClaimTypes.Permission, AppPermissions.CanDeleteUnapprovedRequest));

    options.AddPolicy(AppPermissions.CanManageMasterData, policy =>
        policy.RequireClaim(CustomClaimTypes.Permission, AppPermissions.CanManageMasterData));

    options.AddPolicy(AppPermissions.CanManageRbac, policy =>
        policy.RequireClaim(CustomClaimTypes.Permission, AppPermissions.CanManageRbac));
});

// 4. Blazor Server Interactive Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    });

// 5. Domain Services Registration
builder.Services.AddSingleton<NotificationEventBus>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IFileUploadService, LocalFileUploadService>();
builder.Services.AddScoped<IGoogleFileUploadService, LocalFileUploadService>();
builder.Services.AddScoped<IEmployeeCodeGeneratorService, EmployeeCodeGeneratorService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<IRbacService, RbacService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();

var app = builder.Build();

// 6. Seed Master Data & Initialize Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DbInitializer.InitializeAsync(context, userManager, roleManager);
}

// 7. HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// 8. Auth Minimal API Endpoints (Standard Convention: /Account/Login and /Account/Logout)
app.MapPost("/Account/Login", async (
    HttpContext httpContext,
    [FromServices] SignInManager<ApplicationUser> signInManager,
    [FromServices] UserManager<ApplicationUser> userManager,
    [FromForm] string username,
    [FromForm] string password) =>
{
    var user = await userManager.FindByNameAsync(username);
    if (user != null)
    {
        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return Results.Redirect("/dashboard");
        }
    }
    return Results.Redirect("/Account/Login?error=invalid_credentials");
}).DisableAntiforgery();

app.MapPost("/Account/Logout", async (
    HttpContext httpContext,
    [FromServices] SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/Account/Login");
}).DisableAntiforgery();

// 9. Map Interactive Blazor Server Root Component
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
