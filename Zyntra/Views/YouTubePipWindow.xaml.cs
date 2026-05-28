using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Zyntra.Services;

namespace Zyntra.Views;

public partial class YouTubePipWindow : Window
{
    private readonly string _videoId;

    public YouTubePipWindow(string videoId)
    {
        _videoId = videoId;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Zyntra", "WebView2YouTubePip");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await PipWebView.EnsureCoreWebView2Async(env);

            PipWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            PipWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            PipWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            NavigateToYouTubeEmbed(_videoId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open YouTube PiP.\n\n{ex.Message}",
                "Zyntra", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        DragMove();
    }

    private void MinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        PipWebView?.Dispose();
        base.OnClosed(e);
    }

    private void NavigateToYouTubeEmbed(string videoId)
    {
        string headers = string.Join("\r\n", new[]
        {
            "Referer: https://www.youtube.com/",
            "Origin: https://www.youtube.com",
        });

        var request = PipWebView.CoreWebView2.Environment.CreateWebResourceRequest(
            YouTubeEmbedService.BuildEmbedUrl(videoId),
            "GET",
            null,
            headers);

        PipWebView.CoreWebView2.NavigateWithWebResourceRequest(request);
    }
}
