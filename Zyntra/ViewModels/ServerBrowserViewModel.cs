using System.Collections.ObjectModel;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class ServerBrowserViewModel : BaseViewModel
{
    public ObservableCollection<ServerEntry> Favorites { get; } = new();

    private string _placeIdText = string.Empty;
    public string PlaceIdText
    {
        get => _placeIdText;
        set => SetProperty(ref _placeIdText, value);
    }

    private string _placeName = string.Empty;
    public string PlaceName
    {
        get => _placeName;
        set => SetProperty(ref _placeName, value);
    }

    private string _jobId = string.Empty;
    public string JobId
    {
        get => _jobId;
        set => SetProperty(ref _jobId, value);
    }

    private string _notes = string.Empty;
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    private ServerEntry? _selectedFavorite;
    public ServerEntry? SelectedFavorite
    {
        get => _selectedFavorite;
        set => SetProperty(ref _selectedFavorite, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand AddFavoriteCommand { get; }
    public ICommand RemoveFavoriteCommand { get; }
    public ICommand JoinServerCommand { get; }
    public ICommand CopyJobIdCommand { get; }

    public ServerBrowserViewModel()
    {
        AddFavoriteCommand = new RelayCommand(_ => AddFavorite());
        RemoveFavoriteCommand = new RelayCommand(RemoveFavorite);
        JoinServerCommand = new RelayCommand(JoinServer);
        CopyJobIdCommand = new RelayCommand(CopyJobId);

        LoadFavorites();
    }

    private void LoadFavorites()
    {
        Favorites.Clear();
        foreach (var fav in ServerBrowserService.LoadFavorites())
            Favorites.Add(fav);
        StatusText = $"{Favorites.Count} favorite{(Favorites.Count == 1 ? "" : "s")}";
    }

    private void AddFavorite()
    {
        if (!long.TryParse(_placeIdText.Trim(), out long placeId) || placeId <= 0)
        {
            StatusText = "Enter a valid Place ID";
            return;
        }

        var entry = new ServerEntry
        {
            PlaceId = placeId,
            PlaceName = string.IsNullOrWhiteSpace(_placeName) ? $"Place {placeId}" : _placeName.Trim(),
            JobId = string.IsNullOrWhiteSpace(_jobId) ? null : _jobId.Trim(),
            Notes = string.IsNullOrWhiteSpace(_notes) ? null : _notes.Trim(),
            IsFavorite = true,
        };

        ServerBrowserService.AddFavorite(entry);
        Favorites.Insert(0, entry);

        ActivityLogService.Log("Server Favorited", $"Added {entry.PlaceName} (Place {placeId})", placeId: placeId, placeName: entry.PlaceName);

        PlaceIdText = "";
        PlaceName = "";
        JobId = "";
        Notes = "";
        StatusText = $"Added {entry.PlaceName} to favorites";
    }

    private void RemoveFavorite(object? param)
    {
        var entry = param as ServerEntry ?? SelectedFavorite;
        if (entry == null) return;

        ServerBrowserService.RemoveFavorite(entry.Id);
        Favorites.Remove(entry);
        StatusText = $"Removed {entry.PlaceName}";
    }

    private async void JoinServer(object? param)
    {
        var entry = param as ServerEntry ?? SelectedFavorite;
        if (entry == null) return;

        StatusText = $"Joining {entry.PlaceName}...";
        ActivityLogService.Log("Server Join", $"Joining {entry.PlaceName} (Place {entry.PlaceId})", placeId: entry.PlaceId, placeName: entry.PlaceName);

        try
        {
            await RobloxService.LaunchRobloxAsync(null!, entry.PlaceId);
            StatusText = $"Launched {entry.PlaceName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed: {ex.Message}";
        }
    }

    private void CopyJobId(object? param)
    {
        var entry = param as ServerEntry ?? SelectedFavorite;
        if (entry?.JobId == null) return;

        System.Windows.Clipboard.SetText(entry.JobId);
        StatusText = "Job ID copied!";
    }
}
