using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.Views;

public partial class YouTubePipWindow : Window
{
    private static readonly JsonSerializerOptions MessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _videoId;
    private readonly double _startSeconds;
    private DateTime _lastProgressSave = DateTime.MinValue;
    private bool _isClosed;

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
        _isClosed = true;

        try
        {
            var core = PipWebView.CoreWebView2;
            if (core != null)
                core.WebMessageReceived -= OnWebMessageReceived;
        }
        catch
        {
            // WebView may already be disposing.
        }

        PipWebView?.Dispose();
        base.OnClosed(e);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_isClosed)
            return;

        string json;
        try
        {
            json = e.WebMessageAsJson;
        }
        catch
        {
            return;
        }

        var dispatcher = Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            HandleWebMessage(json);
        else
            dispatcher.BeginInvoke(() => HandleWebMessage(json), DispatcherPriority.Background);
    }

    private void HandleWebMessage(string json)
    {
        if (_isClosed)
            return;

        try
        {
            var message = JsonSerializer.Deserialize<YouTubePlayerMessage>(json, MessageJsonOptions);
            if (message == null || message.Type is not ("ready" or "state" or "progress"))
                return;

            if ((DateTime.UtcNow - _lastProgressSave).TotalSeconds < 3)
                return;

            var library = YouTubeLibraryService.Load();
            var item = library.History.FirstOrDefault(h => h.VideoId == message.VideoId);
            bool isNew = item == null;
            item ??= new YouTubeHistoryItem { VideoId = message.VideoId, WatchCount = 1 };

            item.UpdatePlaybackProgress(
                string.IsNullOrWhiteSpace(message.Title)
                    ? item.Title.Length > 0 ? item.Title : message.VideoId
                    : message.Title,
                message.CurrentTime,
                message.Duration);

            if (isNew)
            {
                library.History.Insert(0, item);
            }

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
