using System.Collections.Concurrent;

namespace WfdHrms.Services;

public class ToastService : IToastService, IDisposable
{
    private readonly object _lock = new();
    private readonly List<ToastMessage> _toasts = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationSources = new();

    public event Action? OnChanged;

    public void ShowSuccess(string message, string? title = "Success")
    {
        AddToast(ToastType.Success, message, title, TimeSpan.FromSeconds(6));
    }

    public void ShowError(string message, string? title = "Error")
    {
        AddToast(ToastType.Error, message, title, TimeSpan.FromSeconds(8));
    }

    public void ShowInfo(string message, string? title = "Notice")
    {
        AddToast(ToastType.Info, message, title, TimeSpan.FromSeconds(6));
    }

    public void ShowWarning(string message, string? title = "Warning")
    {
        AddToast(ToastType.Warning, message, title, TimeSpan.FromSeconds(6));
    }

    public void Dismiss(Guid id)
    {
        lock (_lock)
        {
            _toasts.RemoveAll(t => t.Id == id);
        }

        if (_cancellationSources.TryRemove(id, out var cts))
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignored
            }
        }

        NotifyChanged();
    }

    public IReadOnlyList<ToastMessage> GetActiveToasts()
    {
        lock (_lock)
        {
            return _toasts.ToList();
        }
    }

    private void AddToast(ToastType type, string message, string? title, TimeSpan duration)
    {
        var toast = new ToastMessage(Guid.NewGuid(), type, message, title, DateTime.UtcNow);

        lock (_lock)
        {
            _toasts.Add(toast);
        }

        var cts = new CancellationTokenSource();
        _cancellationSources[toast.Id] = cts;

        NotifyChanged();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(duration, cts.Token);
                Dismiss(toast.Id);
            }
            catch (TaskCanceledException)
            {
                // Expected when dismissed manually
            }
        });
    }

    private void NotifyChanged()
    {
        try
        {
            OnChanged?.Invoke();
        }
        catch
        {
            // Avoid failing toast operations if subscriber throws
        }
    }

    public void Dispose()
    {
        foreach (var cts in _cancellationSources.Values)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch
            {
                // Ignore disposal exceptions
            }
        }
        _cancellationSources.Clear();
    }
}
