using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace WfdHrms.Components.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private string userFullName = "User";
    private string userRolesDisplay = "Staff";

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            userFullName = user.FindFirst("FullName")?.Value ?? user.Identity?.Name ?? "User";
            var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            userRolesDisplay = roles.Count > 0 ? string.Join(", ", roles) : "Staff";
        }
    }
}
