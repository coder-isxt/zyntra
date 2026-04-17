using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Zyntra.Services;

public class RobloxUserInfo
{
    public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public string displayName { get; set; } = string.Empty;
}

public static class RobloxService
{
    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = false,
        };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Zyntra/1.0");
        return client;
    }

    public static async Task<RobloxUserInfo> ValidateCookieAsync(string cookie)
    {
        using var http = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://users.roblox.com/v1/users/authenticated");
        request.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");

        var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Invalid cookie or request failed (HTTP {(int)response.StatusCode}).");

        string json = await response.Content.ReadAsStringAsync();
        var info = JsonSerializer.Deserialize<RobloxUserInfo>(json);
        return info ?? throw new Exception("Failed to parse user info.");
    }

    public static async Task<string> GetAvatarUrlAsync(long userId)
    {
        using var http = CreateClient();
        string url = $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png&isCircular=true";
        var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return string.Empty;

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        if (data.GetArrayLength() > 0)
        {
            var item = data[0];
            if (item.TryGetProperty("imageUrl", out var imageUrl))
                return imageUrl.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public static async Task<string> GetAuthTicketAsync(string cookie)
    {
        using var http = CreateClient();

        // Step 1: POST without CSRF to get the token from the 403 response
        string csrfToken;
        using (var csrfRequest = new HttpRequestMessage(HttpMethod.Post, "https://auth.roblox.com/v1/authentication-ticket"))
        {
            csrfRequest.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
            csrfRequest.Content = new StringContent("");
            csrfRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var csrfResponse = await http.SendAsync(csrfRequest);

            if (!csrfResponse.Headers.TryGetValues("x-csrf-token", out var csrfValues))
                throw new Exception($"Failed to obtain CSRF token (HTTP {(int)csrfResponse.StatusCode}).");

            csrfToken = csrfValues.First();
        }

        // Step 2: POST again with CSRF token to get the actual auth ticket
        using (var ticketRequest = new HttpRequestMessage(HttpMethod.Post, "https://auth.roblox.com/v1/authentication-ticket"))
        {
            ticketRequest.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
            ticketRequest.Headers.Add("x-csrf-token", csrfToken);
            ticketRequest.Headers.TryAddWithoutValidation("Referer", "https://www.roblox.com/");
            ticketRequest.Content = new StringContent("");
            ticketRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var ticketResponse = await http.SendAsync(ticketRequest);

            if (ticketResponse.Headers.TryGetValues("rbx-authentication-ticket", out var ticketValues))
                return ticketValues.First();

            string body = await ticketResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to obtain auth ticket (HTTP {(int)ticketResponse.StatusCode}): {body}");
        }
    }

    public static string? FindRobloxPlayerPath()
    {
        string[] searchRoots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions"),
            @"C:\Program Files (x86)\Roblox\Versions",
            @"C:\Program Files\Roblox\Versions",
        };

        foreach (string root in searchRoots)
        {
            if (!Directory.Exists(root)) continue;

            foreach (string versionDir in Directory.GetDirectories(root))
            {
                string candidate = Path.Combine(versionDir, "RobloxPlayerBeta.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    public static async Task LaunchRobloxAsync(string cookie, long? placeId = null)
    {
        var settings = SettingsService.Load();

        // Resolve player path
        string? playerPath = null;

        // 1. Use custom path from settings if set
        if (!string.IsNullOrEmpty(settings.RobloxVersionPath) && File.Exists(settings.RobloxVersionPath))
        {
            playerPath = settings.RobloxVersionPath;
        }
        // 2. Auto-update: check and download latest if enabled
        else if (settings.AutoUpdateRoblox)
        {
            try
            {
                NotificationService.Push("Roblox", "Checking for latest version...", NotificationType.Info);
                var progress = new Progress<(double progress, string status)>(p =>
                {
                    // Update via toast for visibility
                    if (p.progress < 1.0 && p.progress > 0.02)
                        ToastService.Show("Roblox Update", p.status);
                });
                playerPath = await RobloxVersionService.EnsureLatestVersionAsync(progress);
            }
            catch
            {
                // Fall back to local detection
                playerPath = FindRobloxPlayerPath();
            }
        }
        // 3. Fallback: find any local installation
        else
        {
            playerPath = FindRobloxPlayerPath();
        }

        if (playerPath == null)
            throw new Exception("Roblox Player not found on this computer. Enable 'Auto Update Roblox' in Settings or install Roblox manually.");

        string authTicket = await GetAuthTicketAsync(cookie);
        long launchTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long browserTrackerId = new Random().NextInt64(100000000000, 999999999999);

        if (placeId.HasValue && placeId.Value > 0)
        {
            // Launch into a specific game using roblox-player: protocol
            string placeUrl = Uri.EscapeDataString(
                $"https://assetgame.roblox.com/game/PlaceLauncher.ashx?request=RequestGame&browserTrackerId={browserTrackerId}&placeId={placeId.Value}&isPlayTogetherGame=false");

            string protocolUri =
                $"roblox-player:1+launchmode:play+gameinfo:{authTicket}+launchtime:{launchTime}" +
                $"+placelauncherurl:{placeUrl}" +
                $"+browsertrackerid:{browserTrackerId}" +
                $"+robloxLocale:en_us+gameLocale:en_us+channel:+LaunchExp:InApp";

            Process.Start(new ProcessStartInfo
            {
                FileName = protocolUri,
                UseShellExecute = true,
            });
        }
        else
        {
            // "Just Launch" — open Roblox app directly with auth ticket via exe args
            Process.Start(new ProcessStartInfo
            {
                FileName = playerPath,
                Arguments = $"--app --launchtime={launchTime} -t {authTicket} -a https://www.roblox.com/Login/Negotiate.ashx",
                UseShellExecute = false,
            });
        }
    }
}
