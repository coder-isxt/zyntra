using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class AccentOption
{
    public string Name { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class SettingsViewModel : BaseViewModel
{
    private readonly AppSettings _settings;

    public static string[] PageOptions => new[] { "Roblox", "Apps", "Plugins", "Scripts", "Docs" };

    private bool _launchOnStartup;
    public bool LaunchOnStartup
    {
        get => _launchOnStartup;
        set
        {
            if (SetProperty(ref _launchOnStartup, value))
            {
                _settings.LaunchOnStartup = value;
                SettingsService.Save(_settings);
                SettingsService.SetLaunchOnStartup(value);
            }
        }
    }

    private bool _minimizeToTray;
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            if (SetProperty(ref _minimizeToTray, value))
            {
                _settings.MinimizeToTray = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private string _defaultPage;
    public string DefaultPage
    {
        get => _defaultPage;
        set
        {
            if (SetProperty(ref _defaultPage, value))
            {
                _settings.DefaultPage = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private bool _disableAnimations;
    public bool DisableAnimations
    {
        get => _disableAnimations;
        set
        {
            if (SetProperty(ref _disableAnimations, value))
            {
                _settings.DisableAnimations = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private bool _autoRefreshCookies;
    public bool AutoRefreshCookies
    {
        get => _autoRefreshCookies;
        set
        {
            if (SetProperty(ref _autoRefreshCookies, value))
            {
                _settings.AutoRefreshCookies = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private string _defaultTag;
    public string DefaultTag
    {
        get => _defaultTag;
        set
        {
            if (SetProperty(ref _defaultTag, value))
            {
                _settings.DefaultTag = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private bool _hideInvalidAccounts;
    public bool HideInvalidAccounts
    {
        get => _hideInvalidAccounts;
        set
        {
            if (SetProperty(ref _hideInvalidAccounts, value))
            {
                _settings.HideInvalidAccounts = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private string _defaultScriptTemplate;
    public string DefaultScriptTemplate
    {
        get => _defaultScriptTemplate;
        set
        {
            if (SetProperty(ref _defaultScriptTemplate, value))
            {
                _settings.DefaultScriptTemplate = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private bool _checkForUpdatesOnStartup;
    public bool CheckForUpdatesOnStartup
    {
        get => _checkForUpdatesOnStartup;
        set
        {
            if (SetProperty(ref _checkForUpdatesOnStartup, value))
            {
                _settings.CheckForUpdatesOnStartup = value;
                SettingsService.Save(_settings);
            }
        }
    }

    public ObservableCollection<AccentOption> AccentOptions { get; } = new();

    public ICommand SetAccentCommand { get; }
    public ICommand CheckUpdateCommand { get; }
    public ICommand ExportAccountsCommand { get; }
    public ICommand ImportAccountsCommand { get; }
    public ICommand ClearRecentlyPlayedCommand { get; }

    private string _updateStatus = string.Empty;
    public string UpdateStatus
    {
        get => _updateStatus;
        set => SetProperty(ref _updateStatus, value);
    }

    private bool _isCheckingUpdate;
    public bool IsCheckingUpdate
    {
        get => _isCheckingUpdate;
        set => SetProperty(ref _isCheckingUpdate, value);
    }

    public SettingsViewModel()
    {
        _settings = SettingsService.Load();
        _launchOnStartup = _settings.LaunchOnStartup;
        _minimizeToTray = _settings.MinimizeToTray;
        _defaultPage = _settings.DefaultPage;
        _disableAnimations = _settings.DisableAnimations;
        _autoRefreshCookies = _settings.AutoRefreshCookies;
        _defaultTag = _settings.DefaultTag;
        _hideInvalidAccounts = _settings.HideInvalidAccounts;
        _defaultScriptTemplate = _settings.DefaultScriptTemplate;
        _checkForUpdatesOnStartup = _settings.CheckForUpdatesOnStartup;

        foreach (var (name, hex) in ThemeService.AccentPresets)
        {
            AccentOptions.Add(new AccentOption
            {
                Name = name,
                Hex = hex,
                IsSelected = hex == _settings.AccentColorHex,
            });
        }

        SetAccentCommand = new RelayCommand(SetAccent);
        CheckUpdateCommand = new RelayCommand(async _ => await CheckForUpdateAsync());
        ExportAccountsCommand = new RelayCommand(_ => ExportAccounts());
        ImportAccountsCommand = new RelayCommand(_ => ImportAccounts());
        ClearRecentlyPlayedCommand = new RelayCommand(_ => ClearRecentlyPlayed());

        ThemeService.ApplyAccentColor(_settings.AccentColorHex);
    }

    private void ClearRecentlyPlayed()
    {
        RecentlyPlayedService.Clear();
        NotificationService.Push("Recently Played", "History cleared.", NotificationType.Success);
    }

    private void SetAccent(object? param)
    {
        if (param is not string hex) return;

        _settings.AccentColorHex = hex;
        SettingsService.Save(_settings);
        ThemeService.ApplyAccentColor(hex);

        foreach (var opt in AccentOptions)
            opt.IsSelected = opt.Hex == hex;
        OnPropertyChanged(nameof(AccentOptions));
    }

    private async Task CheckForUpdateAsync()
    {
        IsCheckingUpdate = true;
        UpdateStatus = "Checking for updates...";

        var release = await UpdateService.CheckForUpdateAsync();

        if (release == null)
        {
            UpdateStatus = $"You're on the latest version (v{UpdateService.CurrentVersion})";
            IsCheckingUpdate = false;
            return;
        }

        string version = release.tag_name.TrimStart('v', 'V');
        var result = MessageBox.Show(
            $"A new version is available: v{version}\n\n{release.body}\n\nDownload and install now?",
            "Zyntra Update", MessageBoxButton.YesNo, MessageBoxImage.Information);

        if (result != MessageBoxResult.Yes)
        {
            UpdateStatus = "Update skipped.";
            IsCheckingUpdate = false;
            return;
        }

        UpdateStatus = "Downloading update...";
        var progress = new Progress<double>(p =>
        {
            UpdateStatus = $"Downloading... {p:P0}";
        });

        string? path = await UpdateService.DownloadUpdateAsync(release, progress);
        if (path == null)
        {
            UpdateStatus = "Download failed.";
            IsCheckingUpdate = false;
            return;
        }

        UpdateStatus = "Applying update...";
        UpdateService.ApplyUpdate(path);
    }

    private void ExportAccounts()
    {
        var accounts = AccountStorageService.Load();
        if (accounts.Count == 0)
        {
            MessageBox.Show("No accounts to export.", "Zyntra", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Accounts",
            Filter = "Zyntra Export (*.zyntra)|*.zyntra",
            FileName = "zyntra_accounts.zyntra",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                AccountExportService.Export(accounts, dialog.FileName);
                MessageBox.Show($"Exported {accounts.Count} account(s) successfully.", "Zyntra",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Zyntra",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportAccounts()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Accounts",
            Filter = "Zyntra Export (*.zyntra)|*.zyntra|All Files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var imported = AccountExportService.Import(dialog.FileName);
                if (imported.Count == 0)
                {
                    MessageBox.Show("No accounts found in file.", "Zyntra",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = AccountStorageService.Load();
                int added = 0;
                foreach (var acc in imported)
                {
                    if (!existing.Any(e => e.UserId == acc.UserId))
                    {
                        existing.Add(acc);
                        added++;
                    }
                }
                AccountStorageService.Save(existing);

                MessageBox.Show(
                    $"Imported {added} new account(s). ({imported.Count - added} already existed)",
                    "Zyntra", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}", "Zyntra",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
