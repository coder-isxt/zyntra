using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Zyntra.Models;

public class YouTubeLibraryData
{
    public List<YouTubeHistoryItem> History { get; set; } = new();
    public List<YouTubePlaylist> Playlists { get; set; } = new();
}

public class YouTubeHistoryItem : INotifyPropertyChanged
{
    private string _videoId = string.Empty;
    private string _title = string.Empty;
    private double _lastPositionSeconds;
    private double _durationSeconds;
    private DateTime _lastPlayedAt = DateTime.UtcNow;
    private int _watchCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string VideoId
    {
        get => _videoId;
        set => SetProperty(ref _videoId, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public double LastPositionSeconds
    {
        get => _lastPositionSeconds;
        set
        {
            if (SetProperty(ref _lastPositionSeconds, value))
            {
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ProgressPercent));
            }
        }
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (SetProperty(ref _durationSeconds, value))
            {
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(ProgressPercent));
            }
        }
    }

    public DateTime LastPlayedAt
    {
        get => _lastPlayedAt;
        set => SetProperty(ref _lastPlayedAt, value);
    }

    public int WatchCount
    {
        get => _watchCount;
        set => SetProperty(ref _watchCount, value);
    }

    public string ThumbnailUrl => $"https://img.youtube.com/vi/{VideoId}/mqdefault.jpg";
    public string ProgressText => DurationSeconds > 0
        ? $"{FormatTime(LastPositionSeconds)} / {FormatTime(DurationSeconds)}"
        : FormatTime(LastPositionSeconds);
    public double ProgressPercent => DurationSeconds > 0
        ? Math.Clamp(LastPositionSeconds / DurationSeconds * 100, 0, 100)
        : 0;

    public void UpdatePlaybackProgress(string title, double positionSeconds, double durationSeconds)
    {
        if (!string.IsNullOrWhiteSpace(title) && title != Title)
            Title = title;

        LastPositionSeconds = Math.Max(0, positionSeconds);
        DurationSeconds = Math.Max(DurationSeconds, durationSeconds);
        LastPlayedAt = DateTime.UtcNow;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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
