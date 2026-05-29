using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class YouTubePlayerViewModel : BaseViewModel
{
    private readonly YouTubeLibraryData _library;
    private readonly object _progressLock = new();
    private DateTime _lastProgressSave = DateTime.MinValue;
    private DateTime _lastRecordUtc = DateTime.MinValue;

    public ObservableCollection<YouTubeHistoryItem> History { get; } = new();
    public ObservableCollection<YouTubeHistoryItem> ContinueWatching { get; } = new();
    public ObservableCollection<YouTubePlaylist> Playlists { get; } = new();
    public ObservableCollection<YouTubePlaylistItem> SelectedPlaylistItems { get; } = new();

    private string _activeSection = "Watch";
    public string ActiveSection
    {
        get => _activeSection;
        set
        {
            if (SetProperty(ref _activeSection, value))
            {
                OnPropertyChanged(nameof(IsWatchSection));
                OnPropertyChanged(nameof(IsContinueSection));
                OnPropertyChanged(nameof(IsHistorySection));
                OnPropertyChanged(nameof(IsPlaylistsSection));
            }
        }
    }

    public bool IsWatchSection => ActiveSection == "Watch";
    public bool IsContinueSection => ActiveSection == "Continue";
    public bool IsHistorySection => ActiveSection == "History";
    public bool IsPlaylistsSection => ActiveSection == "Playlists";

    private string _videoInput = string.Empty;
    public string VideoInput
    {
        get => _videoInput;
        set => SetProperty(ref _videoInput, value);
    }

    private string _currentVideoId = string.Empty;
    public string CurrentVideoId
    {
        get => _currentVideoId;
        set => SetProperty(ref _currentVideoId, value);
    }

    private string _currentTitle = string.Empty;
    public string CurrentTitle
    {
        get => _currentTitle;
        set => SetProperty(ref _currentTitle, value);
    }

    private double _currentPositionSeconds;
    public double CurrentPositionSeconds
    {
        get => _currentPositionSeconds;
        set => SetProperty(ref _currentPositionSeconds, value);
    }

    private double _currentDurationSeconds;
    public double CurrentDurationSeconds
    {
        get => _currentDurationSeconds;
        set => SetProperty(ref _currentDurationSeconds, value);
    }

    private string _newPlaylistName = string.Empty;
    public string NewPlaylistName
    {
        get => _newPlaylistName;
        set => SetProperty(ref _newPlaylistName, value);
    }

    private YouTubePlaylist? _selectedPlaylist;
    public YouTubePlaylist? SelectedPlaylist
    {
        get => _selectedPlaylist;
        set
        {
            if (SetProperty(ref _selectedPlaylist, value))
                RefreshSelectedPlaylistItems();
        }
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand LoadVideoCommand { get; }
    public ICommand OpenPipCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand PlayHistoryCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand CreatePlaylistCommand { get; }
    public ICommand DeletePlaylistCommand { get; }
    public ICommand AddCurrentToPlaylistCommand { get; }
    public ICommand PlayPlaylistItemCommand { get; }
    public ICommand RemovePlaylistItemCommand { get; }
    public ICommand NavigateSectionCommand { get; }

    public event Action<string, double>? PlayRequested;
    public event Action? PipRequested;
    public event Action? StopRequested;

    public YouTubePlayerViewModel()
    {
        _library = YouTubeLibraryService.Load();
        LoadLibrary();

        LoadVideoCommand = new RelayCommand(_ => LoadFromInput());
        OpenPipCommand = new RelayCommand(_ => PipRequested?.Invoke());
        StopCommand = new RelayCommand(_ => StopRequested?.Invoke());
        PlayHistoryCommand = new RelayCommand(PlayHistory);
        ClearHistoryCommand = new RelayCommand(_ => ClearHistory());
        CreatePlaylistCommand = new RelayCommand(_ => CreatePlaylist());
        DeletePlaylistCommand = new RelayCommand(_ => DeleteSelectedPlaylist());
        AddCurrentToPlaylistCommand = new RelayCommand(_ => AddCurrentToPlaylist());
        PlayPlaylistItemCommand = new RelayCommand(PlayPlaylistItem);
        RemovePlaylistItemCommand = new RelayCommand(RemovePlaylistItem);
        NavigateSectionCommand = new RelayCommand(p =>
        {
            if (p is not string section)
                return;

            ActiveSection = section;

            if (section == "Continue")
                ScheduleContinueWatchingRefresh();
            else if (section == "History")
                ScheduleHistoryRefresh();
        });
    }

    public void RecordProgress(string videoId, string title, double positionSeconds, double durationSeconds, bool force = false)
    {
        if (string.IsNullOrWhiteSpace(videoId))
            return;

        if (!string.IsNullOrWhiteSpace(CurrentVideoId) &&
            !string.Equals(videoId, CurrentVideoId, StringComparison.Ordinal))
            return;

        positionSeconds = SanitizeSeconds(positionSeconds);
        durationSeconds = SanitizeSeconds(durationSeconds);

        if (!force && (DateTime.UtcNow - _lastRecordUtc).TotalMilliseconds < 350)
            return;

        lock (_progressLock)
        {
            _lastRecordUtc = DateTime.UtcNow;

            CurrentVideoId = videoId;
            CurrentPositionSeconds = positionSeconds;
            CurrentDurationSeconds = Math.Max(CurrentDurationSeconds, durationSeconds);
            if (!string.IsNullOrWhiteSpace(title))
                CurrentTitle = title;

            string resolvedTitle = string.IsNullOrWhiteSpace(title) ? videoId : title;

            var existing = _library.History.FirstOrDefault(h => h.VideoId == videoId);
            bool isNew = existing == null;
            if (isNew)
            {
                existing = new YouTubeHistoryItem { VideoId = videoId, WatchCount = 1 };
                existing.SetPlaybackProgressSilent(resolvedTitle, positionSeconds, durationSeconds);
                _library.History.Insert(0, existing);
                AddHistoryItemToUi(existing);
            }
            else if (existing != null)
            {
                existing.SetPlaybackProgressSilent(
                    string.IsNullOrWhiteSpace(title)
                        ? existing.Title.Length > 0 ? existing.Title : videoId
                        : title,
                    positionSeconds,
                    durationSeconds);
            }

            if (force || isNew || (DateTime.UtcNow - _lastProgressSave).TotalSeconds >= 3)
            {
                SaveLibrary();
                _lastProgressSave = DateTime.UtcNow;
            }

        }
    }

    public void CommitCurrentPlayback()
    {
        if (string.IsNullOrWhiteSpace(CurrentVideoId))
            return;

        RecordProgress(
            CurrentVideoId,
            CurrentTitle,
            CurrentPositionSeconds,
            CurrentDurationSeconds,
            force: true);
    }

    private static double SanitizeSeconds(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            return 0;
        return value;
    }

    private void AddHistoryItemToUi(YouTubeHistoryItem item)
    {
        var existingUi = History.FirstOrDefault(h => h.VideoId == item.VideoId);
        if (existingUi != null)
            History.Remove(existingUi);

        History.Insert(0, item);

        while (History.Count > 50)
            History.RemoveAt(History.Count - 1);
    }

    private void ScheduleContinueWatchingRefresh()
    {
        RunAfterLayout(SyncContinueWatchingToUi);
    }

    private void ScheduleHistoryRefresh()
    {
        RunAfterLayout(() => SyncHistoryCollections(includeContinueWatching: false));
    }

    private static void RunAfterLayout(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            action();
            return;
        }

        dispatcher.BeginInvoke(action, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Rebuilds the Continue Watching list from library data.
    /// Uses display clones so items are not shared with the History sidebar bindings.
    /// </summary>
    private void SyncContinueWatchingToUi()
    {
        var items = _library.History
            .Where(CanResume)
            .OrderByDescending(h => h.LastPlayedAt)
            .Take(5)
            .Select(h => h.CloneForDisplay())
            .ToList();

        ContinueWatching.Clear();
        foreach (var item in items)
            ContinueWatching.Add(item);
    }

    public void SavePlaybackToDisk()
    {
        lock (_progressLock)
        {
            SaveLibrary();
        }
    }

    public void SaveNow()
    {
        SaveLibrary();
    }

    private void LoadFromInput()
    {
        if (!YouTubeEmbedService.TryGetVideoId(VideoInput, out string videoId))
        {
            StatusText = "Enter a valid YouTube link or video ID";
            return;
        }

        var history = _library.History.FirstOrDefault(h => h.VideoId == videoId);
        double startSeconds = GetResumeStart(history);
        ActiveSection = "Watch";
        PlayRequested?.Invoke(videoId, startSeconds);
    }

    private void PlayHistory(object? param)
    {
        if (param is not YouTubeHistoryItem item)
            return;

        ActiveSection = "Watch";
        PlayRequested?.Invoke(item.VideoId, GetResumeStart(item));
    }

    private void PlayPlaylistItem(object? param)
    {
        if (param is not YouTubePlaylistItem item)
            return;

        var history = _library.History.FirstOrDefault(h => h.VideoId == item.VideoId);
        ActiveSection = "Watch";
        PlayRequested?.Invoke(item.VideoId, GetResumeStart(history));
    }

    private void CreatePlaylist()
    {
        string name = NewPlaylistName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = "Enter a playlist name";
            return;
        }

        var playlist = new YouTubePlaylist { Name = name };
        _library.Playlists.Add(playlist);
        Playlists.Add(playlist);
        SelectedPlaylist = playlist;
        NewPlaylistName = string.Empty;
        SaveLibrary();
        StatusText = $"Created playlist: {name}";
    }

    private void DeleteSelectedPlaylist()
    {
        if (SelectedPlaylist == null)
            return;

        string name = SelectedPlaylist.Name;
        _library.Playlists.Remove(SelectedPlaylist);
        Playlists.Remove(SelectedPlaylist);
        SelectedPlaylist = Playlists.FirstOrDefault();
        SaveLibrary();
        StatusText = $"Deleted playlist: {name}";
    }

    private void AddCurrentToPlaylist()
    {
        if (SelectedPlaylist == null)
        {
            StatusText = "Create or select a playlist first";
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentVideoId))
        {
            StatusText = "Load a video before adding it";
            return;
        }

        if (SelectedPlaylist.Items.Any(i => i.VideoId == CurrentVideoId))
        {
            StatusText = "Video is already in this playlist";
            return;
        }

        string title = string.IsNullOrWhiteSpace(CurrentTitle) ? CurrentVideoId : CurrentTitle;
        var item = new YouTubePlaylistItem { VideoId = CurrentVideoId, Title = title };
        SelectedPlaylist.Items.Add(item);
        SelectedPlaylistItems.Add(item);
        SaveLibrary();
        StatusText = $"Added to {SelectedPlaylist.Name}";
    }

    private void RemovePlaylistItem(object? param)
    {
        if (SelectedPlaylist == null || param is not YouTubePlaylistItem item)
            return;

        SelectedPlaylist.Items.Remove(item);
        SelectedPlaylistItems.Remove(item);
        SaveLibrary();
        StatusText = "Removed from playlist";
    }

    private void ClearHistory()
    {
        _library.History.Clear();
        History.Clear();
        ContinueWatching.Clear();
        SaveLibrary();
        StatusText = "History cleared";
    }

    private void LoadLibrary()
    {
        SyncHistoryCollections(includeContinueWatching: false);

        Playlists.Clear();
        foreach (var playlist in _library.Playlists.OrderBy(p => p.CreatedAt))
            Playlists.Add(playlist);

        SelectedPlaylist = Playlists.FirstOrDefault();
    }

    private void SyncHistoryCollections(bool includeContinueWatching = true)
    {
        History.Clear();
        foreach (var item in _library.History.OrderByDescending(h => h.LastPlayedAt).Take(50))
            History.Add(item);

        if (includeContinueWatching)
            SyncContinueWatchingToUi();
    }

    private void RefreshSelectedPlaylistItems()
    {
        SelectedPlaylistItems.Clear();
        if (SelectedPlaylist == null)
            return;

        foreach (var item in SelectedPlaylist.Items.OrderBy(i => i.AddedAt))
            SelectedPlaylistItems.Add(item);
    }

    private void SaveLibrary()
    {
        YouTubeLibraryService.Save(_library);
    }

    private static bool CanResume(YouTubeHistoryItem item)
    {
        if (item.LastPositionSeconds < 15)
            return false;

        if (item.DurationSeconds <= 0)
            return true;

        return item.LastPositionSeconds < item.DurationSeconds - 20;
    }

    private static double GetResumeStart(YouTubeHistoryItem? item)
    {
        if (item == null || !CanResume(item))
            return 0;

        return Math.Max(0, item.LastPositionSeconds - 3);
    }
}
