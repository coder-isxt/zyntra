using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Zyntra.Services;

public static class YouTubeEmbedService
{
    public const string PlayerHostName = "zyntra.youtube.local";

    private static readonly Regex VideoIdRegex = new(@"^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);

    public static bool TryGetVideoId(string input, out string videoId)
    {
        videoId = string.Empty;
        input = input.Trim();

        if (VideoIdRegex.IsMatch(input))
        {
            videoId = input;
            return true;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return false;

        string host = uri.Host.ToLowerInvariant();
        if (host.EndsWith("youtu.be"))
        {
            var id = uri.AbsolutePath.Trim('/').Split('/').FirstOrDefault() ?? string.Empty;
            return SetIfValid(id, out videoId);
        }

        if (!host.EndsWith("youtube.com") && !host.EndsWith("youtube-nocookie.com"))
            return false;

        var query = ParseQuery(uri.Query);
        if (query.TryGetValue("v", out var watchId) && SetIfValid(watchId, out videoId))
            return true;

        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && (parts[0] == "embed" || parts[0] == "shorts" || parts[0] == "live"))
            return SetIfValid(parts[1], out videoId);

        return false;
    }

    public static string EnsurePlayerHostFolder()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Zyntra", "YouTubePlayer");

        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "player.html"), BuildPlayerHostHtml());
        return folder;
    }

    public static string BuildHostedPlayerUrl(string videoId)
    {
        string safeId = WebUtility.UrlEncode(videoId);
        return $"https://{PlayerHostName}/player.html?v={safeId}&player=2";
    }

    private static string BuildEmbedUrl(string videoId)
    {
        string safeId = WebUtility.UrlEncode(videoId);
        string origin = WebUtility.UrlEncode($"https://{PlayerHostName}");
        return $"https://www.youtube.com/embed/{safeId}?autoplay=1&rel=0&modestbranding=1&playsinline=1&origin={origin}&widget_referrer={origin}";
    }

    private static string BuildPlayerHostHtml()
        => $$"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <style>
                html, body {
                  margin: 0;
                  width: 100%;
                  height: 100%;
                  background: #0d1117;
                  overflow: hidden;
                }
                #player, iframe {
                  width: 100%;
                  height: 100%;
                  border: 0;
                  display: block;
                  background: #0d1117;
                }
                .empty {
                  width: 100%;
                  height: 100%;
                  display: grid;
                  place-items: center;
                  color: #99a1b2;
                  font: 14px Segoe UI, sans-serif;
                }
              </style>
            </head>
            <body>
              <div id="player" class="empty">Loading video...</div>
              <script>
                const params = new URLSearchParams(location.search);
                const videoId = params.get("v") || "";
                const valid = /^[A-Za-z0-9_-]{11}$/.test(videoId);
                const root = document.getElementById("player");

                if (valid) {
                  const iframe = document.createElement("iframe");
                  iframe.title = "YouTube video player";
                  iframe.allow = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share";
                  iframe.allowFullscreen = true;
                  iframe.referrerPolicy = "strict-origin-when-cross-origin";
                  iframe.src = "{{BuildEmbedUrl("__VIDEO_ID__")}}".replace("__VIDEO_ID__", encodeURIComponent(videoId));
                  root.className = "";
                  root.textContent = "";
                  root.appendChild(iframe);
                } else {
                  root.textContent = "Invalid YouTube video ID.";
                }
              </script>
            </body>
            </html>
            """;

    private static bool SetIfValid(string value, out string videoId)
    {
        videoId = value.Trim();
        if (VideoIdRegex.IsMatch(videoId))
            return true;

        videoId = string.Empty;
        return false;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                result[WebUtility.UrlDecode(parts[0])] = WebUtility.UrlDecode(parts[1]);
        }
        return result;
    }
}
