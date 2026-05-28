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

    public static string BuildHostedPlayerUrl(string videoId, double startSeconds = 0)
    {
        string safeId = WebUtility.UrlEncode(videoId);
        int start = Math.Max(0, (int)Math.Floor(startSeconds));
        return $"https://{PlayerHostName}/player.html?v={safeId}&t={start}&player=3";
    }

    private static string BuildPlayerHostHtml()
        => """
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
              <script src="https://www.youtube.com/iframe_api"></script>
              <script>
                const params = new URLSearchParams(location.search);
                const videoId = params.get("v") || "";
                const startSeconds = Math.max(0, parseInt(params.get("t") || "0", 10) || 0);
                const valid = /^[A-Za-z0-9_-]{11}$/.test(videoId);
                const root = document.getElementById("player");
                let player = null;
                let progressTimer = null;

                function send(type, extra) {
                  const payload = Object.assign({
                    type,
                    videoId,
                    title: "",
                    currentTime: 0,
                    duration: 0,
                    state: -1
                  }, extra || {});

                  if (player && typeof player.getCurrentTime === "function") {
                    try {
                      const data = player.getVideoData ? player.getVideoData() : null;
                      payload.title = data && data.title ? data.title : payload.title;
                      payload.currentTime = player.getCurrentTime() || 0;
                      payload.duration = player.getDuration ? player.getDuration() || 0 : 0;
                      payload.state = player.getPlayerState ? player.getPlayerState() : -1;
                    } catch {}
                  }

                  if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(payload);
                  }
                }

                function startProgressTimer() {
                  if (progressTimer) return;
                  progressTimer = setInterval(() => send("progress"), 3000);
                }

                function stopProgressTimer() {
                  if (!progressTimer) return;
                  clearInterval(progressTimer);
                  progressTimer = null;
                }

                function onYouTubeIframeAPIReady() {
                  if (!valid) {
                    root.textContent = "Invalid YouTube video ID.";
                    send("error", { errorCode: "invalid-id" });
                    return;
                  }

                  root.className = "";
                  root.textContent = "";
                  player = new YT.Player("player", {
                    width: "100%",
                    height: "100%",
                    videoId,
                    playerVars: {
                      autoplay: 1,
                      rel: 0,
                      modestbranding: 1,
                      playsinline: 1,
                      start: startSeconds,
                      origin: location.origin,
                      widget_referrer: location.origin
                    },
                    events: {
                      onReady: () => {
                        send("ready");
                        startProgressTimer();
                      },
                      onStateChange: (event) => {
                        send("state", { state: event.data });
                        if (event.data === YT.PlayerState.PLAYING) startProgressTimer();
                        if (event.data === YT.PlayerState.ENDED) stopProgressTimer();
                      },
                      onError: (event) => send("error", { errorCode: event.data })
                    }
                  });
                }

                window.addEventListener("beforeunload", () => send("progress"));

                if (!valid) {
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
