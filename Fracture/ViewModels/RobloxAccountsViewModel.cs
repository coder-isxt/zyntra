using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Fracture.Models;
using Fracture.Services;

namespace Fracture.ViewModels;

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
    public ICommand JoinGameCommand { get; }
    public ICommand RefreshAccountCommand { get; }
    public ICommand CheckHealthCommand { get; }
    public ICommand SetTagCommand { get; }
    public ICommand SetNoteCommand { get; }
    public ICommand ImportAccountsCommand { get; }

    public RobloxAccountsViewModel()
    {
        AddAccountCommand = new RelayCommand(_ => { }, _ => true);
        RemoveAccountCommand = new RelayCommand(async p => await RemoveAccountAsync(p));
        LaunchRobloxCommand = new RelayCommand(async p => await LaunchRobloxAsync(p));
        JoinGameCommand = new RelayCommand(async p => await JoinGameAsync(p));
        RefreshAccountCommand = new RelayCommand(async p => await RefreshAccountAsync(p));
        CheckHealthCommand = new RelayCommand(async _ => await CheckAllHealthAsync());
        SetTagCommand = new RelayCommand(SetTag);
        SetNoteCommand = new RelayCommand(SetNote);
        ImportAccountsCommand = new RelayCommand(async _ => await ImportAccountsAsync());

        LoadAccounts();
        RecentlyPlayedService.Load();
        ActivityLogService.Load();
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

    private void SetNote(object? param)
    {
        if (param is not RobloxAccount account) return;

        var input = new Views.NotesInputWindow(account.Username, account.Notes)
        {
            Owner = Application.Current.MainWindow,
        };
        if (input.ShowDialog() != true) return;

        account.Notes = input.NotesResult;
        SaveAccounts();
        RefreshAccountRow(account);
        StatusText = $"Updated note for {account.Username}";
    }

    private async Task ImportAccountsAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import accounts (.txt / .csv of .ROBLOSECURITY cookies)",
            Filter = "Text/CSV (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*",
        };
        if (dialog.ShowDialog() != true) return;

        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not read file: {ex.Message}", "Fracture", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var cookies = ExtractCookies(lines);
        if (cookies.Count == 0)
        {
            StatusText = "No cookies found in file";
            MessageBox.Show("No .ROBLOSECURITY cookies were found in that file.\n\n" +
                "Expected one cookie per line, or a CSV column containing the cookie.",
                "Fracture", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsLoading = true;
        int ok = 0, fail = 0, i = 0;
        foreach (var cookie in cookies)
        {
            i++;
            StatusText = $"Importing accounts... ({i}/{cookies.Count})";
            try
            {
                await AddAccountWithCookieAsync(cookie, silent: true);
                ok++;
            }
            catch
            {
                fail++;
            }
        }
        IsLoading = false;

        ActivityLogService.Log(ActivityKind.Import, $"Imported {ok} account(s) from file",
            fail > 0 ? $"{fail} failed" : null);
        StatusText = $"Import done: {ok} added/updated" + (fail > 0 ? $", {fail} failed" : "");
    }

    private static List<string> ExtractCookies(IEnumerable<string> lines)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();

        foreach (var raw in lines)
        {
            string line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Find the cookie token in a plain or comma/semicolon/tab separated line.
            string candidate = line;
            foreach (var sep in new[] { ',', ';', '\t' })
            {
                var parts = line.Split(sep);
                var match = parts.FirstOrDefault(p => p.Contains("_|WARNING", StringComparison.OrdinalIgnoreCase))
                            ?? parts.OrderByDescending(p => p.Trim().Length).FirstOrDefault();
                if (match != null && match.Contains("_|WARNING", StringComparison.OrdinalIgnoreCase))
                {
                    candidate = match;
                    break;
                }
            }

            candidate = candidate.Trim().Trim('"');
            if (candidate.Length < 40) continue; // too short to be a real cookie

            if (seen.Add(candidate))
                result.Add(candidate);
        }

        return result;
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
                ActivityLogService.Log(ActivityKind.HealthCheck,
                    $"Cookie health check: {valid} valid, {invalid} expired");
                StatusText = $"Health check done: {valid} valid, {invalid} expired";
                IsLoading = false;
            });
    }

    public async Task AddAccountWithCookieAsync(string cookie, bool silent = false)
    {
        cookie = cookie.Trim();
        if (cookie.StartsWith("_|WARNING:-DO-NOT-SHARE-THIS.--Sharing-this-will-allow-someone-to-log-in-as-you-and-to-steal-your-ROBUX-and-items.|_"))
        {
            // Cookie includes the warning prefix, keep as-is
        }

        if (!silent) IsLoading = true;
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
                ActivityLogService.Log(ActivityKind.AccountAdded, $"Added account {userInfo.name}");
            }

            SaveAccounts();
            RebuildTags();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusText = $"Failed: {ex.Message}";
            if (silent) throw;
            MessageBox.Show($"Failed to validate cookie: {ex.Message}", "Fracture", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (!silent) IsLoading = false;
        }
    }

    private Task RemoveAccountAsync(object? param)
    {
        var account = param as RobloxAccount ?? SelectedAccount;
        if (account == null) return Task.CompletedTask;

        var result = MessageBox.Show(
            $"Remove account '{account.Username}'?",
            "Fracture", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Accounts.Remove(account);
            SaveAccounts();
            ApplyFilter();
            ActivityLogService.Log(ActivityKind.AccountRemoved, $"Removed account {account.Username}");
            StatusText = $"Removed {account.Username}";
        }

        return Task.CompletedTask;
    }

    private async Task LaunchRobloxAsync(object? param)
    {
        var account = param as RobloxAccount ?? SelectedAccount;
        if (account == null) return;
        await LaunchAccountAsync(account, null, null);
    }

    private async Task JoinGameAsync(object? param)
    {
        var account = param as RobloxAccount ?? SelectedAccount;
        if (account == null) return;

        var prompt = new Views.LaunchPromptWindow
        {
            Owner = Application.Current.MainWindow,
            AccountName = string.IsNullOrEmpty(account.DisplayName) ? account.Username : account.DisplayName,
        };

        if (prompt.ShowDialog() != true)
            return;

        if (prompt.JustLaunch)
        {
            await LaunchAccountAsync(account, null, null);
            return;
        }

        long? placeId = prompt.PlaceId;
        string? jobId = prompt.JobId;

        // Join by username: resolve the user's current server via presence.
        if (!string.IsNullOrWhiteSpace(prompt.TargetUsername))
        {
            IsLoading = true;
            StatusText = $"Finding {prompt.TargetUsername}...";
            try
            {
                long? userId = await RobloxService.ResolveUsernameAsync(prompt.TargetUsername.Trim());
                if (userId == null)
                {
                    StatusText = $"User '{prompt.TargetUsername}' not found";
                    MessageBox.Show($"Could not find a Roblox user named '{prompt.TargetUsername}'.",
                        "Fracture", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string cookie = CryptoService.Decrypt(account.EncryptedCookie);
                var presence = await RobloxService.GetUserPresenceAsync(cookie, userId.Value);
                if (presence == null || !presence.InGame)
                {
                    StatusText = $"{prompt.TargetUsername} is not currently in a joinable game";
                    MessageBox.Show($"{prompt.TargetUsername} is not currently in a game, or their join settings are private.",
                        "Fracture", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                placeId = presence.PlaceId;
                jobId = presence.JobId;
            }
            catch (Exception ex)
            {
                StatusText = $"Failed to find user: {ex.Message}";
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        await LaunchAccountAsync(account, placeId, jobId);
    }

    private async Task LaunchAccountAsync(RobloxAccount account, long? placeId, string? jobId)
    {
        IsLoading = true;
        StatusText = placeId.HasValue
            ? $"Launching {account.Username} into game..."
            : $"Launching Roblox as {account.Username}...";

        try
        {
            string cookie = CryptoService.Decrypt(account.EncryptedCookie);
            var process = await RobloxService.LaunchRobloxAsync(cookie, placeId, jobId);

            account.SessionCount++;
            account.LastPlayedAt = DateTime.Now;
            SaveAccounts();
            RefreshAccountRow(account);

            ActivityLogService.Log(ActivityKind.Launch,
                placeId.HasValue
                    ? $"Launched {account.Username} into place {placeId}"
                    : $"Launched Roblox as {account.Username}");

            ActivityTrackerService.Track(account, process, () =>
            {
                SaveAccounts();
                RefreshAccountRow(account);
            });

            if (placeId.HasValue)
                _ = RecentlyPlayedService.AddGameAsync(placeId.Value, account.Username);

            StatusText = $"Roblox launched as {account.Username}";
        }
        catch (Exception ex)
        {
            StatusText = $"Launch failed: {ex.Message}";
            MessageBox.Show($"Failed to launch Roblox: {ex.Message}", "Fracture", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshAccountRow(RobloxAccount account)
    {
        int idx = Accounts.IndexOf(account);
        if (idx >= 0) { Accounts.RemoveAt(idx); Accounts.Insert(idx, account); }
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
