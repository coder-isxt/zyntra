using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Fracture.Services;
using Fracture.ViewModels;

namespace Fracture.Views;

public partial class YouTubePlayerView : UserControl
{
    private static readonly JsonSerializerOptions MessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private bool _webViewReady;
    private bool _isUnloaded;
    private int _playbackGeneration;

    public YouTubePlayerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isUnloaded = false;
        WireViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is YouTubePlayerViewModel oldVm)
        {
            oldVm.PlayRequested -= PlayVideo;
            oldVm.PipRequested -= OpenPip;
            oldVm.StopRequested -= StopVideo;
        }

        WireViewModel();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isUnloaded = true;

        if (DataContext is YouTubePlayerViewModel vm)
        {
            vm.PlayRequested -= PlayVideo;
            vm.PipRequested -= OpenPip;
            vm.StopRequested -= StopVideo;
            vm.SavePlaybackToDisk();
        }

        TeardownWebView();
    }

    private void TeardownWebView()
    {
        if (!_webViewReady)
            return;

        try
        {
            var core = PlayerWebView.CoreWebView2;
            if (core != null)
            {
                core.WebMessageReceived -= OnWebMessageReceived;
                core.NavigateToString(BuildBlankPage());
            }
        }
        catch
        {
            // WebView may already be disposing.
        }

        _webViewReady = false;
    }

    private void WireViewModel()
    {
        if (DataContext is not YouTubePlayerViewModel vm)
            return;

        vm.PlayRequested -= PlayVideo;
        vm.PipRequested -= OpenPip;
        vm.StopRequested -= StopVideo;

        vm.PlayRequested += PlayVideo;
        vm.PipRequested += OpenPip;
        vm.StopRequested += StopVideo;
    }

    private async Task InitializeWebViewAsync()
    {
        if (_webViewReady || _isUnloaded)
            return;

        try
        {
            await YouTubeWebViewEnvironment.EnsurePlayerReadyAsync(PlayerWebView);

            if (_isUnloaded)
                return;

            var core = PlayerWebView.CoreWebView2!;
            core.WebMessageReceived -= OnWebMessageReceived;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.WebMessageReceived += OnWebMessageReceived;

            _webViewReady = true;
        }
        catch (Exception ex)
        {
            if (DataContext is YouTubePlayerViewModel vm)
                vm.StatusText = "WebView2 failed to start";

            MessageBox.Show(
                $"Failed to initialize YouTube player. Make sure WebView2 Runtime is installed.\n\n{ex.Message}",
                "Fracture", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void PlayVideo(string videoId, double startSeconds)
    {
        if (DataContext is not YouTubePlayerViewModel vm)
            return;

        await InitializeWebViewAsync();
        if (!_webViewReady || _isUnloaded)
            return;

        vm.CommitCurrentPlayback();

        _playbackGeneration++;
        int generation = _playbackGeneration;

        vm.CurrentVideoId = videoId;
        vm.StatusText = startSeconds > 0
            ? $"Continuing {videoId}"
            : $"Playing {videoId}";
        EmptyOverlay.Visibility = Visibility.Collapsed;

        try
        {
            PlayerWebView.CoreWebView2.Navigate(
                YouTubeEmbedService.BuildHostedPlayerUrl(videoId, startSeconds));
        }
        catch
        {
            if (generation == _playbackGeneration)
                vm.StatusText = "Failed to load video";
        }
    }

    private void OpenPip()
    {
        if (DataContext is not YouTubePlayerViewModel vm)
            return;

        string videoId = vm.CurrentVideoId;
        if (string.IsNullOrWhiteSpace(videoId) &&
            !YouTubeEmbedService.TryGetVideoId(vm.VideoInput, out videoId))
        {
            vm.StatusText = "Load a video before opening PiP";
            return;
        }

        var window = new YouTubePipWindow(videoId, vm.CurrentPositionSeconds)
        {
            Owner = Window.GetWindow(this)
        };
        window.Show();

        if (_webViewReady)
        {
            PlayerWebView.NavigateToString(BuildBlankPage());
            EmptyOverlay.Visibility = Visibility.Visible;
        }

        vm.StatusText = "PiP window opened";
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_isUnloaded)
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
        if (_isUnloaded || DataContext is not YouTubePlayerViewModel vm)
            return;

        try
        {
            var message = JsonSerializer.Deserialize<YouTubePlayerMessage>(json, MessageJsonOptions);
            if (message == null)
                return;

            if (!string.IsNullOrWhiteSpace(vm.CurrentVideoId) &&
                !string.Equals(message.VideoId, vm.CurrentVideoId, StringComparison.Ordinal))
                return;

            if (message.Type is "ready" or "state" or "progress")
            {
                vm.RecordProgress(
                    message.VideoId,
                    message.Title,
                    message.CurrentTime,
                    message.Duration,
                    force: message.Force);
            }
            else if (message.Type == "error")
            {
                vm.StatusText = $"YouTube player error: {message.ErrorCode}";
            }
        }
        catch
        {
            // Ignore malformed script messages from the hosted player.
        }
    }

    private void StopVideo()
    {
        if (DataContext is YouTubePlayerViewModel vm)
        {
            vm.CommitCurrentPlayback();
            vm.CurrentVideoId = string.Empty;
            vm.StatusText = "Stopped";
        }

        _playbackGeneration++;

        if (_webViewReady)
            PlayerWebView.NavigateToString(BuildBlankPage());

        EmptyOverlay.Visibility = Visibility.Visible;
    }

    private static string BuildBlankPage()
        => "<html><body style=\"margin:0;background:#0d1117\"></body></html>";

    private sealed class YouTubePlayerMessage
    {
        public string Type { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
        public int State { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public bool Force { get; set; }
    }
}
