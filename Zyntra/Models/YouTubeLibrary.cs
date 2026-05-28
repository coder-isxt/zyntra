namespace Zyntra.Models;

public class YouTubeLibraryData
{
    public List<YouTubeHistoryItem> History { get; set; } = new();
    public List<YouTubePlaylist> Playlists { get; set; } = new();
}

public class YouTubeHistoryItem
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double LastPositionSeconds { get; set; }
    public double DurationSeconds { get; set; }
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
    public int WatchCount { get; set; }
    public string ThumbnailUrl => $"https://img.youtube.com/vi/{VideoId}/mqdefault.jpg";
    public string ProgressText => DurationSeconds > 0
        ? $"{FormatTime(LastPositionSeconds)} / {FormatTime(DurationSeconds)}"
        : FormatTime(LastPositionSeconds);
    public double ProgressPercent => DurationSeconds > 0
        ? Math.Clamp(LastPositionSeconds / DurationSeconds * 100, 0, 100)
        : 0;

    private static string FormatTime(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes}:{time.Seconds:00}";
    }
}

public class YouTubePlaylist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<YouTubePlaylistItem> Items { get; set; } = new();
}

public class YouTubePlaylistItem
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string ThumbnailUrl => $"https://img.youtube.com/vi/{VideoId}/mqdefault.jpg";
}
