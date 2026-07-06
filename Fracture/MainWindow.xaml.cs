using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Fracture.Services;
using Fracture.ViewModels;

namespace Fracture;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTrayIcon();
        VersionText.Text = $"Fracture v{UpdateService.CurrentVersion}";

        if (DataContext is MainViewModel vm)
        {
            // Animate page transitions (unless disabled)
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentPage) && !vm.SettingsVM.DisableAnimations)
                    AnimatePageTransition();
            };

            // Highlight the correct sidebar button for the default page
            Loaded += (_, _) =>
            {
                var tag = vm.SettingsVM.DefaultPage;
                var activeStyle = (Style)FindResource("SidebarActiveButtonStyle");
                var normalStyle = (Style)FindResource("SidebarButtonStyle");
                BtnApps.Style = tag == "Apps" ? activeStyle : normalStyle;
                BtnRoblox.Style = tag == "Roblox" ? activeStyle : normalStyle;
                BtnSettings.Style = tag == "Settings" ? activeStyle : normalStyle;
            };

            // Check for updates on startup if enabled
            if (vm.SettingsVM.CheckForUpdatesOnStartup)
                _ = CheckForUpdateOnStartupAsync(vm);

            // Auto-refresh cookies if enabled
            if (vm.SettingsVM.AutoRefreshCookies)
                vm.RobloxVM.CheckHealthCommand.Execute(null);
        }
    }

    private async Task CheckForUpdateOnStartupAsync(MainViewModel vm)
    {
        await Task.Delay(2000); // Wait for UI to settle
        var release = await UpdateService.CheckForUpdateAsync();
        if (release != null)
        {
            string version = release.tag_name.TrimStart('v', 'V');
            var result = MessageBox.Show(
                $"A new version is available: v{version}\n\n{release.body}\n\nDownload and install now?",
                "Fracture Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                var path = await UpdateService.DownloadUpdateAsync(release);
                if (path != null) UpdateService.ApplyUpdate(path);
            }
        }
    }

    private void AnimatePageTransition()
    {
        PageHost.RenderTransform = new System.Windows.Media.TranslateTransform(0, 0);
        PageHost.Opacity = 0;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var slideIn = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        PageHost.BeginAnimation(OpacityProperty, fadeIn);
        PageHost.RenderTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideIn);
    }

    private void InitializeTrayIcon()
    {
        var iconStream = System.Windows.Application.GetResourceStream(new Uri("logo.ico", UriKind.Relative));
        if (iconStream != null)
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon(iconStream.Stream),
                Text = "Fracture",
                Visible = false,
            };
            _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

            BuildTrayMenu();
        }
    }

    private void BuildTrayMenu()
    {
        if (_trayIcon == null) return;

        var menu = new System.Windows.Forms.ContextMenuStrip();

        menu.Items.Add("Show Fracture", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        // Accounts submenu
        var vm = DataContext as MainViewModel;
        var accounts = vm?.RobloxVM.Accounts;
        if (accounts != null && accounts.Count > 0)
        {
            var accountsMenu = new System.Windows.Forms.ToolStripMenuItem("Accounts");
            foreach (var account in accounts)
            {
                var acc = account;
                accountsMenu.DropDownItems.Add(acc.DisplayName, null, async (_, _) =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            string cookie = CryptoService.Decrypt(acc.EncryptedCookie);
                            await RobloxService.LaunchRobloxAsync(cookie);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Launch failed: {ex.Message}", "Fracture",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                });
            }
            menu.Items.Add(accountsMenu);
        }
        else
        {
            var noAccounts = new System.Windows.Forms.ToolStripMenuItem("No accounts added");
            noAccounts.Enabled = false;
            menu.Items.Add(noAccounts);
        }

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) =>
        {
            _trayIcon!.Visible = false;
            _trayIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        });

        _trayIcon.ContextMenuStrip = menu;
    }

    private void RestoreFromTray()
    {
        if (_trayIcon != null)
            _trayIcon.Visible = false;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeClick(sender, e);
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        bool minimizeToTray = vm?.SettingsVM.MinimizeToTray ?? false;

        if (minimizeToTray && _trayIcon != null)
        {
            BuildTrayMenu();
            _trayIcon.Visible = true;
            Hide();
        }
        else
        {
            _trayIcon?.Dispose();
            Close();
        }
    }

    private void SidebarNav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clicked)
        {
            string tag = clicked.Tag as string ?? "Apps";

            var activeStyle = (Style)FindResource("SidebarActiveButtonStyle");
            var normalStyle = (Style)FindResource("SidebarButtonStyle");

            BtnApps.Style = tag == "Apps" ? activeStyle : normalStyle;
            BtnRoblox.Style = tag == "Roblox" ? activeStyle : normalStyle;
            BtnSettings.Style = tag == "Settings" ? activeStyle : normalStyle;
        }
    }
}
