using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Zyntra.Services;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class YouTubePlayerView : UserControl
{
    private bool _webViewReady;

    public YouTubePlayerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
        WireViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is YouTubePlayerViewModel oldVm)
        {
            oldVm.LoadRequested -= LoadVideo;
            oldVm.PipRequested -= OpenPip;
            oldVm.StopRequested -= StopVideo;
        }

        WireViewModel();
    }

    private void WireViewModel()
    {
        if (DataContext is not YouTubePlayerViewModel vm)
            return;

        vm.LoadRequested -= LoadVideo;
        vm.PipRequested -= OpenPip;
        vm.StopRequested -= StopVideo;

        vm.LoadRequested += LoadVideo;
        vm.PipRequested += OpenPip;
        vm.StopRequested += StopVideo;
    }

    private async Task InitializeWebViewAsync()
    {
        if (_webViewReady)
            return;

        try
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Zyntra", "WebView2YouTube");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await PlayerWebView.EnsureCoreWebView2Async(env);

            PlayerWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            PlayerWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            PlayerWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            _webViewReady = true;
        }
        catch (Exception ex)
        {
            if (DataContext is YouTubePlayerViewModel vm)
                vm.StatusText = "WebView2 failed to start";

            MessageBox.Show(
                $"Failed to initialize YouTube player. Make sure WebView2 Runtime is installed.\n\n{ex.Message}",
                "Zyntra", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadVideo()
    {
        if (DataContext is not YouTubePlayerViewModel vm)
            return;

        await InitializeWebViewAsync();
        if (!_webViewReady)
            return;

        if (!YouTubeEmbedService.TryGetVideoId(vm.VideoInput, out string videoId))
        {
            vm.StatusText = "Enter a valid YouTube link or video ID";
            return;
        }

        vm.CurrentVideoId = videoId;
        vm.StatusText = $"Playing {videoId}";
        EmptyOverlay.Visibility = Visibility.Collapsed;
        PlayerWebView.NavigateToString(YouTubeEmbedService.BuildPlayerHtml(videoId));
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

        var window = new YouTubePipWindow(videoId)
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

    private void StopVideo()
    {
        if (_webViewReady)
            PlayerWebView.NavigateToString(BuildBlankPage());

        EmptyOverlay.Visibility = Visibility.Visible;

        if (DataContext is YouTubePlayerViewModel vm)
        {
            vm.CurrentVideoId = string.Empty;
            vm.StatusText = "Stopped";
        }
    }

    private static string BuildBlankPage()
        => "<html><body style=\"margin:0;background:#0d1117\"></body></html>";
}
