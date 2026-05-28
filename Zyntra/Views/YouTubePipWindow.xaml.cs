using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.Views;

public partial class YouTubePipWindow : Window
{
    private readonly string _videoId;
    private readonly double _startSeconds;
    private DateTime _lastProgressSave = DateTime.MinValue;

    public YouTubePipWindow(string videoId, double startSeconds = 0)
    {
        _videoId = videoId;
        _startSeconds = startSeconds;
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

            string playerHostFolder = YouTubeEmbedService.EnsurePlayerHostFolder();
            PipWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                YouTubeEmbedService.PlayerHostName,
                playerHostFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            PipWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            PipWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            PipWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            PipWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            PipWebView.CoreWebView2.Navigate(YouTubeEmbedService.BuildHostedPlayerUrl(_videoId, _startSeconds));
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

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = JsonSerializer.Deserialize<YouTubePlayerMessage>(
                e.WebMessageAsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (message == null || message.Type is not ("ready" or "state" or "progress"))
                return;

            if ((DateTime.UtcNow - _lastProgressSave).TotalSeconds < 3)
                return;

            var library = YouTubeLibraryService.Load();
            var item = library.History.FirstOrDefault(h => h.VideoId == message.VideoId);
            item ??= new YouTubeHistoryItem { VideoId = message.VideoId, WatchCount = 1 };

            item.Title = string.IsNullOrWhiteSpace(message.Title)
                ? item.Title.Length > 0 ? item.Title : message.VideoId
                : message.Title;
            item.LastPositionSeconds = Math.Max(0, message.CurrentTime);
            item.DurationSeconds = Math.Max(item.DurationSeconds, message.Duration);
            item.LastPlayedAt = DateTime.UtcNow;

            library.History.Remove(item);
            library.History.Insert(0, item);
            YouTubeLibraryService.Save(library);
            _lastProgressSave = DateTime.UtcNow;
        }
        catch
        {
            // Ignore malformed script messages from the hosted player.
        }
    }

    private sealed class YouTubePlayerMessage
    {
        public string Type { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
    }
}
