using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class RobloxAccountsViewModel : BaseViewModel
{
    public ObservableCollection<RobloxAccount> Accounts { get; } = new();

    private RobloxAccount? _selectedAccount;
    public RobloxAccount? SelectedAccount
    {
        get => _selectedAccount;
        set => SetProperty(ref _selectedAccount, value);
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

    public RobloxAccountsViewModel()
    {
        AddAccountCommand = new RelayCommand(_ => { }, _ => true);
        RemoveAccountCommand = new RelayCommand(async p => await RemoveAccountAsync(p), _ => SelectedAccount != null);
        LaunchRobloxCommand = new RelayCommand(async p => await LaunchRobloxAsync(p), _ => SelectedAccount != null);
        RefreshAccountCommand = new RelayCommand(async p => await RefreshAccountAsync(p), _ => SelectedAccount != null);

        LoadAccounts();
    }

    private void LoadAccounts()
    {
        Accounts.Clear();
        var saved = AccountStorageService.Load();
        foreach (var acc in saved)
            Accounts.Add(acc);
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
                var account = new RobloxAccount
                {
                    UserId = userInfo.id,
                    Username = userInfo.name,
                    DisplayName = userInfo.displayName,
                    EncryptedCookie = CryptoService.Encrypt(cookie),
                    AvatarUrl = avatarUrl,
                    AddedAt = DateTime.UtcNow,
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
            StatusText = prompt.JustLaunch
                ? $"Roblox launched as {account.Username}"
                : $"Roblox launched as {account.Username} (Place {prompt.PlaceId})";
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
