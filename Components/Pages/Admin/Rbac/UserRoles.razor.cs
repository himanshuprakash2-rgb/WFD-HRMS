using Microsoft.AspNetCore.Components;
using WfdHrms.Services;

namespace WfdHrms.Components.Pages.Admin.Rbac;

public partial class UserRoles : ComponentBase
{
    [Inject] private IRbacService RbacService { get; set; } = null!;

    private List<UserDto> users = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        users = await RbacService.GetAllUsersWithRolesAsync();
    }
}
