using System.Net.Http;
using System.Xml.Linq;

namespace Fracture.Services;

public record YouTubePlaylistVideo(string VideoId, string Title);

public record YouTubePlaylistFetchResult(string PlaylistTitle, List<YouTubePlaylistVideo> Videos);

public static class YouTubePlaylistService
{
    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "Fracture-App" } }
    };

    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace Yt = "http://www.youtube.com/xml/schemas/2015";

    public static async Task<YouTubePlaylistFetchResult> FetchPlaylistAsync(string playlistId)
    {
        string url = $"https://www.youtube.com/feeds/videos.xml?playlist_id={Uri.EscapeDataString(playlistId)}";
        string xml = await _http.GetStringAsync(url);
        var doc = XDocument.Parse(xml);

        string playlistTitle = doc.Root?.Element(Atom + "title")?.Value ?? "Imported Playlist";

        var videos = new List<YouTubePlaylistVideo>();
        foreach (var entry in doc.Root?.Elements(Atom + "entry") ?? Enumerable.Empty<XElement>())
        {
            string videoId = entry.Element(Yt + "videoId")?.Value?.Trim() ?? string.Empty;
            string title = entry.Element(Atom + "title")?.Value?.Trim() ?? videoId;

            if (!string.IsNullOrWhiteSpace(videoId))
                videos.Add(new YouTubePlaylistVideo(videoId, title));
        }

        return new YouTubePlaylistFetchResult(playlistTitle, videos);
    }
}
