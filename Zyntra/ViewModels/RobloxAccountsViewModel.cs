using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class RobloxAccountsViewModel : BaseViewModel
{
    public ObservableCollection<RobloxAccount> Accounts { get; } = new();
    public ObservableCollection<RobloxAccount> FilteredAccounts { get; } = new();
    public ObservableCollection<string> AvailableTags { get; } = new();

    public ObservableCollection<Models.RecentGame> RecentGames => RecentlyPlayedService.Games;

    private RobloxAccount? _selectedAccount;
    public RobloxAccount? SelectedAccount
    {
        get => _selectedAccount;
        set => SetProperty(ref _selectedAccount, value);
    }

    private string _selectedTag = "All";
    public string SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (SetProperty(ref _selectedTag, value))
                ApplyFilter();
        }
    }

    private bool _isGridView;
    public bool IsGridView
    {
        get => _isGridView;
        set => SetProperty(ref _isGridView, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand AddAccountCommand { get; }
    public ICommand RemoveAccountCommand { get; }
    public ICommand LaunchRobloxCommand { get; }
    public ICommand RefreshAccountCommand { get; }
    public ICommand CheckHealthCommand { get; }
    public ICommand SetTagCommand { get; }

    public RobloxAccountsViewModel()
    {
        AddAccountCommand = new RelayCommand(_ => { }, _ => true);
        RemoveAccountCommand = new RelayCommand(async p => await RemoveAccountAsync(p));
        LaunchRobloxCommand = new RelayCommand(async p => await LaunchRobloxAsync(p));
        RefreshAccountCommand = new RelayCommand(async p => await RefreshAccountAsync(p));
        CheckHealthCommand = new RelayCommand(async _ => await CheckAllHealthAsync());
        SetTagCommand = new RelayCommand(SetTag);

        LoadAccounts();
        RecentlyPlayedService.Load();
    }

    private void LoadAccounts()
    {
        Accounts.Clear();
        var saved = AccountStorageService.Load();
        foreach (var acc in saved)
            Accounts.Add(acc);
        RebuildTags();
        ApplyFilter();
    }

    private void RebuildTags()
    {
        AvailableTags.Clear();
        AvailableTags.Add("All");
        var tags = Accounts.Select(a => a.Tag).Where(t => !string.IsNullOrEmpty(t)).Distinct().OrderBy(t => t);
        foreach (var tag in tags)
            AvailableTags.Add(tag);
    }

    private void ApplyFilter()
    {
        FilteredAccounts.Clear();
        var source = _selectedTag == "All"
            ? Accounts.AsEnumerable()
            : Accounts.Where(a => a.Tag == _selectedTag);

        var settings = SettingsService.Load();
        if (settings.HideInvalidAccounts)
            source = source.Where(a => a.CookieValid != false);

        foreach (var acc in source)
            FilteredAccounts.Add(acc);
    }

    private void SetTag(object? param)
    {
        if (param is not RobloxAccount account) return;

        string currentTag = account.Tag;
        string? newTag = PromptForTag(currentTag);
        if (newTag == null) return;

        account.Tag = newTag;
        SaveAccounts();
        RebuildTags();
        ApplyFilter();

        int idx = Accounts.IndexOf(account);
        if (idx >= 0) { Accounts.RemoveAt(idx); Accounts.Insert(idx, account); }

        StatusText = string.IsNullOrEmpty(newTag)
            ? $"Removed tag from {account.Username}"
            : $"Tagged {account.Username} as \"{newTag}\"";
    }

    private static string? PromptForTag(string currentTag)
    {
        var input = new Views.TagInputWindow(currentTag);
        input.Owner = Application.Current.MainWindow;
        return input.ShowDialog() == true ? input.TagResult : null;
    }

    private async Task CheckAllHealthAsync()
    {
        IsLoading = true;
        StatusText = "Checking cookie health...";
        int valid = 0, invalid = 0;

        await CookieHealthService.CheckAllAccountsAsync(
            Accounts.ToList(),
            (account, isValid) =>
            {
                if (isValid) valid++; else invalid++;
                StatusText = $"Checking... ({valid + invalid}/{Accounts.Count})";

                int idx = Accounts.IndexOf(account);
                if (idx >= 0) { Accounts.RemoveAt(idx); Accounts.Insert(idx, account); }
            },
            () =>
            {
                SaveAccounts();
                ApplyFilter();
                StatusText = $"Health check done: {valid} valid, {invalid} expired";
                IsLoading = false;
            });
    }

    public async Task AddAccountWithCookieAsync(string cookie)
    {
        cookie = cookie.Trim();
        if (cookie.StartsWith("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_"))
        {
            // Cookie includes the warning prefix, keep as-is
        }

        IsLoading = true;
        StatusText = "Validating cookie...";

        try
        {
            var userInfo = await RobloxService.ValidateCookieAsync(cookie);

            string avatarUrl = await RobloxService.GetAvatarUrlAsync(userInfo.id);

            if (Accounts.Any(a => a.UserId == userInfo.id))
            {
                var existing = Accounts.First(a => a.UserId == userInfo.id);
                existing.EncryptedCookie = CryptoService.Encrypt(cookie);
                existing.Username = userInfo.name;
                existing.DisplayName = userInfo.displayName;
                existing.AvatarUrl = avatarUrl;

                int idx = Accounts.IndexOf(existing);
                if (idx >= 0) { Accounts.RemoveAt(idx); Accounts.Insert(idx, existing); }

                StatusText = $"Updated account: {userInfo.name}";
            }
            else
            {
                var settings = SettingsService.Load();
                var account = new RobloxAccount
                {
                    UserId = userInfo.id,
                    Username = userInfo.name,
                    DisplayName = userInfo.displayName,
                    EncryptedCookie = CryptoService.Encrypt(cookie),
                    AvatarUrl = avatarUrl,
                    AddedAt = DateTime.UtcNow,
                    Tag = settings.DefaultTag,
                };
                Accounts.Add(account);
                StatusText = $"Added account: {userInfo.name}";
            }

            SaveAccounts();
        }
        catch (Exception ex)
        {
            StatusText = $"Failed: {ex.Message}";
            MessageBox.Show($"Failed to validate cookie: {ex.Message}", "Zyntra", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task RemoveAccountAsync(object? param)
    {
        var account = param as RobloxAccount ?? SelectedAccount;
        if (account == null) return Task.CompletedTask;

        var result = MessageBox.Show(
            $"Remove account '{account.Username}'?",
            "Zyntra", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Accounts.Remove(account);
            SaveAccounts();
            StatusText = $"Removed {account.Username}";
        }

        return Task.CompletedTask;
    }

    private async Task LaunchRobloxAsync(object? param)
    {
        var account = param as RobloxAccount ?? SelectedAccount;
        if (account == null) return;

        var prompt = new Views.LaunchPromptWindow
        {
            Owner = Application.Current.MainWindow,
            AccountName = account.DisplayName,
        };

        if (prompt.ShowDialog() != true) return;

        IsLoading = true;
        StatusText = $"Launching Roblox as {account.Username}...";

        try
        {
            string cookie = CryptoService.Decrypt(account.EncryptedCookie);
            await RobloxService.LaunchRobloxAsync(cookie, prompt.PlaceId);

            if (!prompt.JustLaunch && prompt.PlaceId.HasValue)
            {
                StatusText = $"Resolving game name...";
                await RecentlyPlayedService.AddGameAsync(prompt.PlaceId.Value, account.DisplayName);
                var latest = RecentlyPlayedService.Games.FirstOrDefault();
                StatusText = latest != null
                    ? $"Launched {latest.GameName} as {account.Username}"
                    : $"Roblox launched as {account.Username} (Place {prompt.PlaceId})";
                OnPropertyChanged(nameof(RecentGames));
            }
            else
            {
                StatusText = $"Roblox launched as {account.Username}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Launch failed: {ex.Message}";
            MessageBox.Show($"Failed to launch Roblox: {ex.Message}", "Zyntra", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAccountAsync(object? param)
    {
        var account = param as RobloxAccount ?? SelectedAccount;
        if (account == null) return;

        IsLoading = true;
        StatusText = $"Refreshing {account.Username}...";

        try
        {
            string cookie = CryptoService.Decrypt(account.EncryptedCookie);
            var userInfo = await RobloxService.ValidateCookieAsync(cookie);
            string avatarUrl = await RobloxService.GetAvatarUrlAsync(userInfo.id);
            account.Username = userInfo.name;
            account.DisplayName = userInfo.displayName;
            account.AvatarUrl = avatarUrl;
            SaveAccounts();

            int idx = Accounts.IndexOf(account);
            if (idx >= 0)
            {
                Accounts.RemoveAt(idx);
                Accounts.Insert(idx, account);
            }

            StatusText = $"Refreshed {account.Username}";
        }
        catch (Exception ex)
        {
            StatusText = $"Refresh failed (cookie may be expired): {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SaveAccounts()
    {
        AccountStorageService.Save(Accounts.ToList());
    }
}
