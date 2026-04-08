using System.Collections.ObjectModel;

namespace Zyntra.Services;

public enum NotificationType { Info, Success, Warning, Error }

public class NotificationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
}

public static class NotificationService
{
    public static ObservableCollection<NotificationItem> Notifications { get; } = new();

    public static int UnreadCount => Notifications.Count(n => !n.IsRead);

    public static event Action? OnChanged;

    public static void Push(string title, string message, NotificationType type = NotificationType.Info)
    {
        var item = new NotificationItem { Title = title, Message = message, Type = type };

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Notifications.Insert(0, item);
            // Keep max 50 notifications
            while (Notifications.Count > 50)
                Notifications.RemoveAt(Notifications.Count - 1);
            OnChanged?.Invoke();
        });
    }

    public static void MarkAllRead()
    {
        foreach (var n in Notifications)
            n.IsRead = true;
        OnChanged?.Invoke();
    }

    public static void Clear()
    {
        Notifications.Clear();
        OnChanged?.Invoke();
    }
}
