using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Zyntra.Services;

public class GitHubRelease
{
    public string tag_name { get; set; } = string.Empty;
    public string html_url { get; set; } = string.Empty;
    public string body { get; set; } = string.Empty;
    public List<GitHubAsset> assets { get; set; } = new();
}

public class GitHubAsset
{
    public string name { get; set; } = string.Empty;
    public string browser_download_url { get; set; } = string.Empty;
    public long size { get; set; }
}

public static class UpdateService
{
    public const string CurrentVersion = "1.0.7";
    public const string GitHubRepo = "coder-isxt/zyntra";

    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "Zyntra-Updater" } }
    };

    public static async Task<GitHubRelease?> CheckForUpdateAsync()
    {
        try
        {
            string url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release == null) return null;

            string remoteVersion = release.tag_name.TrimStart('v', 'V');
            if (IsNewerVersion(remoteVersion, CurrentVersion))
                return release;

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> DownloadUpdateAsync(GitHubRelease release, IProgress<double>? progress = null)
    {
        var asset = release.assets.FirstOrDefault(a => a.name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (asset == null) return null;

        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "Zyntra_Update.exe");

            using var response = await _http.GetAsync(asset.browser_download_url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? asset.size;
            long downloadedBytes = 0;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;
                if (totalBytes > 0)
                    progress?.Report((double)downloadedBytes / totalBytes);
            }

            return tempPath;
        }
        catch
        {
            return null;
        }
    }

    public static void ApplyUpdate(string downloadedExePath)
    {
        string currentExe = Environment.ProcessPath ?? string.Empty;
        if (string.IsNullOrEmpty(currentExe)) return;

        string batchPath = Path.Combine(Path.GetTempPath(), "zyntra_update.bat");
        string batch = $"""
            @echo off
            timeout /t 2 /nobreak >nul
            move /y "{downloadedExePath}" "{currentExe}"
            start "" "{currentExe}"
            del "%~f0"
            """;

        File.WriteAllText(batchPath, batch);

        Process.Start(new ProcessStartInfo
        {
            FileName = batchPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        });

        Application.Current.Shutdown();
    }

    private static bool IsNewerVersion(string remote, string current)
    {
        if (Version.TryParse(remote, out var remoteVer) && Version.TryParse(current, out var currentVer))
            return remoteVer > currentVer;
        return false;
    }
}
