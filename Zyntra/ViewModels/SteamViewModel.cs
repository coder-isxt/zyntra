using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class SteamViewModel : BaseViewModel
{
    public ObservableCollection<string> KnownUsers { get; } = new();

    private string? _currentUser;
    public string? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    private string? _selectedUser;
    public string? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private bool _steamInstalled;
    public bool SteamInstalled
    {
        get => _steamInstalled;
        set => SetProperty(ref _steamInstalled, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SwitchAccountCommand { get; }
    public ICommand RestartSteamCommand { get; }
    public ICommand LaunchGameCommand { get; }

    public SteamViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Refresh());
        SwitchAccountCommand = new RelayCommand(SwitchAccount);
        RestartSteamCommand = new RelayCommand(_ => RestartSteam());
        LaunchGameCommand = new RelayCommand(_ => LaunchGame());

        Refresh();
    }

    private void Refresh()
    {
        string? steamPath = SteamService.GetSteamPath();
        SteamInstalled = steamPath != null;

        if (!SteamInstalled)
        {
            StatusText = "Steam not detected.";
            return;
        }

        CurrentUser = SteamService.GetCurrentUser();
        KnownUsers.Clear();
        foreach (var user in SteamService.GetKnownUsers())
            KnownUsers.Add(user);

        StatusText = KnownUsers.Count > 0
            ? $"Found {KnownUsers.Count} Steam account(s). Current: {CurrentUser ?? "none"}"
            : "No saved Steam accounts found.";
    }

    private void SwitchAccount(object? param)
    {
        string? user = param as string ?? SelectedUser;
        if (string.IsNullOrEmpty(user)) return;

        SteamService.SwitchAccount(user);
        CurrentUser = user;
        StatusText = $"Switched to {user}. Restart Steam to apply.";
    }

    private void RestartSteam()
    {
        string? user = SelectedUser;
        StatusText = user != null ? $"Restarting Steam as {user}..." : "Restarting Steam...";

        try
        {
            SteamService.RestartSteam(user);
            if (user != null) CurrentUser = user;
            StatusText = "Steam restarting...";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed: {ex.Message}";
        }
    }

    private void LaunchGame()
    {
        var prompt = new Views.SteamGamePromptWindow();
        prompt.Owner = Application.Current.MainWindow;

        if (prompt.ShowDialog() == true && prompt.AppId > 0)
        {
            try
            {
                SteamService.LaunchGame(prompt.AppId, SelectedUser);
                StatusText = $"Launching Steam app {prompt.AppId}...";
            }
            catch (Exception ex)
            {
                StatusText = $"Launch failed: {ex.Message}";
            }
        }
    }
}
