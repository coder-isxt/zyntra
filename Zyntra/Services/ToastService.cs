using System.Collections.ObjectModel;

namespace Zyntra.Services;

public class ToastItem
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public static class ToastService
{
    public static ObservableCollection<ToastItem> ActiveToasts { get; } = new();

    public static void Show(string title, string message, NotificationType type = NotificationType.Info)
    {
        var toast = new ToastItem { Title = title, Message = message, Type = type };

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            ActiveToasts.Insert(0, toast);
            // Max 3 visible toasts
            while (ActiveToasts.Count > 3)
                ActiveToasts.RemoveAt(ActiveToasts.Count - 1);
        });

        // Auto-dismiss after 4 seconds
        _ = DismissAfterDelay(toast, 4000);
    }

    private static async Task DismissAfterDelay(ToastItem toast, int ms)
    {
        await Task.Delay(ms);
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            ActiveToasts.Remove(toast);
        });
    }
}
