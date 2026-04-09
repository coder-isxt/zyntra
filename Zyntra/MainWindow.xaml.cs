using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Zyntra.Services;
using Zyntra.ViewModels;

namespace Zyntra;

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
        VersionText.Text = $"Zyntra v{UpdateService.CurrentVersion}";

        // Animate page transitions
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentPage))
                    AnimatePageTransition();
            };
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
                Text = "Zyntra",
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

        menu.Items.Add("Show Zyntra", null, (_, _) => RestoreFromTray());
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
                        var prompt = new Views.LaunchPromptWindow
                        {
                            AccountName = acc.DisplayName,
                        };

                        // Show on top even when in tray
                        prompt.Topmost = true;
                        prompt.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                        if (prompt.ShowDialog() == true)
                        {
                            try
                            {
                                string cookie = CryptoService.Decrypt(acc.EncryptedCookie);
                                await RobloxService.LaunchRobloxAsync(cookie, prompt.PlaceId);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Launch failed: {ex.Message}", "Zyntra",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
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

            BtnDashboard.Style = tag == "Dashboard" ? activeStyle : normalStyle;
            BtnApps.Style = tag == "Apps" ? activeStyle : normalStyle;
            BtnRoblox.Style = tag == "Roblox" ? activeStyle : normalStyle;
            BtnServers.Style = tag == "Servers" ? activeStyle : normalStyle;
            BtnActivityLog.Style = tag == "ActivityLog" ? activeStyle : normalStyle;
            BtnPlugins.Style = tag == "Plugins" ? activeStyle : normalStyle;
            BtnScripts.Style = tag == "Scripts" ? activeStyle : normalStyle;
            BtnDocs.Style = tag == "Docs" ? activeStyle : normalStyle;
            BtnSettings.Style = tag == "Settings" ? activeStyle : normalStyle;
        }
    }
}