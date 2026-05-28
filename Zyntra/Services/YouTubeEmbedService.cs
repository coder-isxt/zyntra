using System.Net;
using System.Text.RegularExpressions;

namespace Zyntra.Services;

public static class YouTubeEmbedService
{
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

    public static string BuildEmbedUrl(string videoId)
    {
        string safeId = WebUtility.UrlEncode(videoId);
        return $"https://www.youtube-nocookie.com/embed/{safeId}?autoplay=1&rel=0&modestbranding=1&playsinline=1";
    }

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
