using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace WfdHrms.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [SupplyParameterFromQuery]
    private string? Error { get; set; }

    private bool hasLoginError = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            Navigation.NavigateTo("/dashboard", replace: true);
            return;
        }

        hasLoginError = !string.IsNullOrEmpty(Error);
    }
}
