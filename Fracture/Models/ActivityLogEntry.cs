namespace Fracture.Models;

public enum ActivityKind
{
    Launch,
    SessionEnd,
    AccountAdded,
    AccountRemoved,
    HealthCheck,
    AppLaunch,
    Import,
    Info
}

public class ActivityLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public ActivityKind Kind { get; set; } = ActivityKind.Info;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }

    /// <summary>Emoji icon shown in the activity list, derived from the kind.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Icon => Kind switch
    {
        ActivityKind.Launch => "🚀",
        ActivityKind.SessionEnd => "⏱️",
        ActivityKind.AccountAdded => "➕",
        ActivityKind.AccountRemoved => "➖",
        ActivityKind.HealthCheck => "❤️",
        ActivityKind.AppLaunch => "📦",
        ActivityKind.Import => "📥",
        _ => "•"
    };

    [System.Text.Json.Serialization.JsonIgnore]
    public string TimeText => Timestamp.ToString("MMM d, HH:mm");
}
