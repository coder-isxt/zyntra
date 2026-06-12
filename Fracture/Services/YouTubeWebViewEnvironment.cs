using System.IO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Fracture.Services;

/// <summary>
/// Single shared WebView2 environment for the YouTube player.
/// Creating multiple environments for the same user-data folder is unsupported and can crash.
/// </summary>
public static class YouTubeWebViewEnvironment
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static CoreWebView2Environment? _environment;
    private static bool _virtualHostMapped;

    public static async Task EnsurePlayerReadyAsync(WebView2 webView)
    {
        var env = await GetOrCreateEnvironmentAsync().ConfigureAwait(true);
        await webView.EnsureCoreWebView2Async(env).ConfigureAwait(true);

        var core = webView.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 failed to initialize.");

        if (_virtualHostMapped)
            return;

        string playerHostFolder = YouTubeEmbedService.EnsurePlayerHostFolder();
        core.SetVirtualHostNameToFolderMapping(
            YouTubeEmbedService.PlayerHostName,
            playerHostFolder,
            CoreWebView2HostResourceAccessKind.Allow);

        _virtualHostMapped = true;
    }

    private static async Task<CoreWebView2Environment> GetOrCreateEnvironmentAsync()
    {
        if (_environment != null)
            return _environment;

        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_environment != null)
                return _environment;

            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Fracture", "WebView2YouTube");

            _environment = await CoreWebView2Environment
                .CreateAsync(null, userDataFolder)
                .ConfigureAwait(false);

            return _environment;
        }
        finally
        {
            Gate.Release();
        }
    }
}
