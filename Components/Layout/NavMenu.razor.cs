using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using WfdHrms.Data.Entities;
using WfdHrms.Services;

namespace WfdHrms.Components.Layout;

public partial class NavMenu : ComponentBase, IDisposable
{
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IOnboardingService OnboardingService { get; set; } = null!;
    [Inject] private NotificationEventBus NotificationBus { get; set; } = null!;
    [Inject] private IToastService ToastService { get; set; } = null!;

    private string? currentUserId;
    private int unreadCount = 0;
    private bool showNotificationPanel = false;
    private List<InAppNotification> notifications = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                await ReloadNotificationsAsync();
                NotificationBus.NotificationReceived += HandleNotificationReceivedAsync;
            }
        }
    }

    private async Task ReloadNotificationsAsync()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;
        unreadCount = await OnboardingService.GetUnreadNotificationCountAsync(currentUserId);
        notifications = await OnboardingService.GetUserNotificationsAsync(currentUserId);
    }

    private async Task HandleNotificationReceivedAsync(string recipientUserId, InAppNotification notification)
    {
        if (recipientUserId == currentUserId)
        {
            unreadCount++;
            notifications.Insert(0, notification);
            ToastService.ShowInfo(notification.Message, "In-App Notification");
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ToggleNotifications()
    {
        showNotificationPanel = !showNotificationPanel;
    }

    private async Task MarkAsReadAsync(long notificationId)
    {
        await OnboardingService.MarkNotificationAsReadAsync(notificationId);
        var item = notifications.FirstOrDefault(n => n.Id == notificationId);
        if (item != null)
        {
            item.IsRead = true;
            unreadCount = Math.Max(0, unreadCount - 1);
        }
    }

    public void Dispose()
    {
        NotificationBus.NotificationReceived -= HandleNotificationReceivedAsync;
    }
}
