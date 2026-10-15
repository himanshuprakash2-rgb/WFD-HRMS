using WfdHrms.Data.Entities;

namespace WfdHrms.Services;

public class NotificationEventBus
{
    public event Func<string, InAppNotification, Task>? NotificationReceived;

    public async Task PublishAsync(string recipientUserId, InAppNotification notification)
    {
        if (NotificationReceived != null)
        {
            var delegates = NotificationReceived.GetInvocationList();
            foreach (Func<string, InAppNotification, Task> handler in delegates)
            {
                try
                {
                    await handler.Invoke(recipientUserId, notification);
                }
                catch
                {
                    // Ignore transient delegate exceptions to prevent breaking publisher flow
                }
            }
        }
    }
}
