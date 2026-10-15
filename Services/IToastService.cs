namespace WfdHrms.Services;

public enum ToastType
{
    Success,
    Error,
    Info,
    Warning
}

public record ToastMessage(
    Guid Id,
    ToastType Type,
    string Message,
    string? Title,
    DateTime CreatedAt);

public interface IToastService
{
    event Action? OnChanged;

    void ShowSuccess(string message, string? title = "Success");
    void ShowError(string message, string? title = "Error");
    void ShowInfo(string message, string? title = "Notice");
    void ShowWarning(string message, string? title = "Warning");
    void Dismiss(Guid id);
    IReadOnlyList<ToastMessage> GetActiveToasts();
}
