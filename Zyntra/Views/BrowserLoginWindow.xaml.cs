using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace Zyntra.Views;

public partial class BrowserLoginWindow : Window
{
    public string? CapturedCookie { get; private set; }

    private bool _cookieCaptured;
    private DispatcherTimer? _cookieTimer;

    public BrowserLoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Zyntra", "WebView2");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await LoginWebView.EnsureCoreWebView2Async(env);

            LoginWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            LoginWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            LoginWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            LoginWebView.NavigationCompleted += OnNavigationCompleted;

            // Use DispatcherTimer so cookie checks run on the UI thread
            _cookieTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _cookieTimer.Tick += async (s, args) => await CheckForCookieAsync();
            _cookieTimer.Start();

            LoginWebView.CoreWebView2.Navigate("https://www.roblox.com/login");

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize browser. Make sure WebView2 Runtime is installed.\n\n{ex.Message}",
                "Zyntra", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Check cookie on every navigation in case the timer hasn't caught it
        await CheckForCookieAsync();
    }

    private async Task CheckForCookieAsync()
    {
        if (_cookieCaptured) return;

        try
        {
            var cookieManager = LoginWebView.CoreWebView2.CookieManager;

            // Check both domains — Roblox sets the cookie on .roblox.com
            var cookies = await cookieManager.GetCookiesAsync("https://www.roblox.com");

            var robloSecurity = cookies.FirstOrDefault(c =>
                c.Name == ".ROBLOSECURITY" && !string.IsNullOrWhiteSpace(c.Value));

            if (robloSecurity == null)
            {
                // Also try the bare domain
                cookies = await cookieManager.GetCookiesAsync("https://roblox.com");
                robloSecurity = cookies.FirstOrDefault(c =>
                    c.Name == ".ROBLOSECURITY" && !string.IsNullOrWhiteSpace(c.Value));
            }

            if (robloSecurity != null)
            {
                _cookieCaptured = true;
                _cookieTimer?.Stop();

                CapturedCookie = robloSecurity.Value;

                // Hide browser immediately, show saving status
                LoginWebView.Visibility = Visibility.Collapsed;
                LoadingOverlay.Visibility = Visibility.Visible;
                StatusLabel.Text = "Login successful! Saving account...";

                // Clear all cookies from WebView2 so session doesn't linger
                cookieManager.DeleteAllCookies();

                DialogResult = true;
                Close();
            }
        }
        catch
        {
            // WebView2 may not be fully ready, ignore
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        DragMove();
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        _cookieTimer?.Stop();
        DialogResult = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _cookieTimer?.Stop();
        LoginWebView?.Dispose();
        base.OnClosed(e);
    }
}
