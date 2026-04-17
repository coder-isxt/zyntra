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

    public static string[] PageOptions => new[] { "Roblox", "Apps", "Scripts", "Docs" };

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

    private bool _showSidebarBadges;
    public bool ShowSidebarBadges
    {
        get => _showSidebarBadges;
        set
        {
            if (SetProperty(ref _showSidebarBadges, value))
            {
                _settings.ShowSidebarBadges = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private bool _autoUpdateRoblox;
    public bool AutoUpdateRoblox
    {
        get => _autoUpdateRoblox;
        set
        {
            if (SetProperty(ref _autoUpdateRoblox, value))
            {
                _settings.AutoUpdateRoblox = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private string _robloxVersionPath;
    public string RobloxVersionPath
    {
        get => _robloxVersionPath;
        set
        {
            if (SetProperty(ref _robloxVersionPath, value))
            {
                _settings.RobloxVersionPath = value;
                SettingsService.Save(_settings);
            }
        }
    }

    private ObservableCollection<string> _availableRobloxVersions = new();
    public ObservableCollection<string> AvailableRobloxVersions
    {
        get => _availableRobloxVersions;
        set => SetProperty(ref _availableRobloxVersions, value);
    }

    private string _selectedRobloxVersion = string.Empty;
    public string SelectedRobloxVersion
    {
        get => _selectedRobloxVersion;
        set
        {
            if (SetProperty(ref _selectedRobloxVersion, value) && !string.IsNullOrEmpty(value))
            {
                if (value == "(auto-detect latest)")
                {
                    RobloxVersionPath = string.Empty;
                }
                else if (value == "Browse folder...")
                {
                    BrowseRobloxFolder();
                }
                else
                {
                    // A specific version hash was selected — find its actual path
                    var path = RobloxVersionService.FindVersionPath(value);
                    RobloxVersionPath = path ?? string.Empty;
                }
            }
        }
    }

    private string _robloxVersionStatus = string.Empty;
    public string RobloxVersionStatus
    {
        get => _robloxVersionStatus;
        set => SetProperty(ref _robloxVersionStatus, value);
    }

    public ObservableCollection<AccentOption> AccentOptions { get; } = new();

    public ICommand SetAccentCommand { get; }
    public ICommand CheckUpdateCommand { get; }
    public ICommand ExportAccountsCommand { get; }
    public ICommand ImportAccountsCommand { get; }
    public ICommand ClearRecentlyPlayedCommand { get; }
    public ICommand CheckRobloxVersionCommand { get; }
    public ICommand BrowseRobloxFolderCommand { get; }

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
        _showSidebarBadges = _settings.ShowSidebarBadges;
        _autoUpdateRoblox = _settings.AutoUpdateRoblox;
        _robloxVersionPath = _settings.RobloxVersionPath;

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
        CheckRobloxVersionCommand = new RelayCommand(async _ => await CheckRobloxVersionAsync());
        BrowseRobloxFolderCommand = new RelayCommand(_ => BrowseRobloxFolder());

        RefreshRobloxVersions();

        ThemeService.ApplyAccentColor(_settings.AccentColorHex);
    }

    public void RefreshRobloxVersions()
    {
        AvailableRobloxVersions.Clear();
        AvailableRobloxVersions.Add("(auto-detect latest)");

        foreach (var v in RobloxVersionService.GetLocalVersions())
            AvailableRobloxVersions.Add(v);

        AvailableRobloxVersions.Add("Browse folder...");

        // Set selection
        if (string.IsNullOrEmpty(_robloxVersionPath))
        {
            _selectedRobloxVersion = "(auto-detect latest)";
        }
        else
        {
            // Try to find a matching version in the list
            var dirName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(_robloxVersionPath) ?? "");
            if (AvailableRobloxVersions.Contains(dirName))
                _selectedRobloxVersion = dirName;
            else
                _selectedRobloxVersion = "(auto-detect latest)";
        }
        OnPropertyChanged(nameof(SelectedRobloxVersion));
    }

    private async Task CheckRobloxVersionAsync()
    {
        RobloxVersionStatus = "Checking...";
        try
        {
            var info = await RobloxVersionService.GetLatestVersionAsync();
            if (info == null)
            {
                RobloxVersionStatus = "Failed to check (no internet?)";
                return;
            }

            string? existing = RobloxVersionService.FindVersionPath(info.clientVersionUpload);
            if (existing != null)
            {
                RobloxVersionStatus = $"Up to date: {info.version} ({info.clientVersionUpload})";
            }
            else
            {
                RobloxVersionStatus = $"New version available: {info.version}";
                if (AutoUpdateRoblox)
                {
                    RobloxVersionStatus = $"Downloading {info.version}...";
                    var progress = new Progress<(double progress, string status)>(p =>
                    {
                        RobloxVersionStatus = p.status;
                    });
                    await RobloxVersionService.DownloadVersionAsync(info.clientVersionUpload, progress);
                    RobloxVersionStatus = $"Installed: {info.version}";
                    RefreshRobloxVersions();
                    NotificationService.Push("Roblox Updated", $"Roblox {info.version} installed.", NotificationType.Success);
                }
            }
        }
        catch (Exception ex)
        {
            RobloxVersionStatus = $"Error: {ex.Message}";
        }
    }

    private void BrowseRobloxFolder()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select RobloxPlayerBeta.exe",
            Filter = "Roblox Player|RobloxPlayerBeta.exe|All Files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            RobloxVersionPath = dialog.FileName;
            RobloxVersionStatus = $"Custom: {dialog.FileName}";
            // Reset dropdown to auto since custom path is set directly
            _selectedRobloxVersion = "(auto-detect latest)";
            OnPropertyChanged(nameof(SelectedRobloxVersion));
        }
        else
        {
            // User cancelled — reset to auto
            _selectedRobloxVersion = "(auto-detect latest)";
            OnPropertyChanged(nameof(SelectedRobloxVersion));
        }
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
