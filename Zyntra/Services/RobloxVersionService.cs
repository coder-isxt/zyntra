using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace Zyntra.Services;

public class RobloxVersionInfo
{
    public string version { get; set; } = string.Empty;
    public string clientVersionUpload { get; set; } = string.Empty;
    public int bootstrapperVersion { get; set; }
}

public static class RobloxVersionService
{
    private static readonly string VersionsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zyntra", "roblox-versions");

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    static RobloxVersionService()
    {
        _http.DefaultRequestHeaders.Add("User-Agent", "Zyntra/1.0");
    }

    /// <summary>
    /// Fetches the latest Roblox player version info from Roblox CDN.
    /// </summary>
    public static async Task<RobloxVersionInfo?> GetLatestVersionAsync()
    {
        try
        {
            var response = await _http.GetAsync(
                "https://clientsettingscdn.roblox.com/v2/client-version/WindowsPlayer");
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RobloxVersionInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Lists all locally available Roblox version hashes in the managed versions folder.
    /// </summary>
    public static List<string> GetLocalVersions()
    {
        var versions = new List<string>();

        if (Directory.Exists(VersionsDir))
        {
            foreach (var dir in Directory.GetDirectories(VersionsDir))
            {
                string exe = Path.Combine(dir, "RobloxPlayerBeta.exe");
                if (File.Exists(exe))
                    versions.Add(Path.GetFileName(dir));
            }
        }

        // Also scan standard Roblox installation
        string[] standardRoots =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions"),
            @"C:\Program Files (x86)\Roblox\Versions",
            @"C:\Program Files\Roblox\Versions",
        };

        foreach (string root in standardRoots)
        {
            if (!Directory.Exists(root)) continue;
            foreach (string versionDir in Directory.GetDirectories(root))
            {
                string name = Path.GetFileName(versionDir);
                if (!versions.Contains(name) && File.Exists(Path.Combine(versionDir, "RobloxPlayerBeta.exe")))
                    versions.Add(name);
            }
        }

        return versions;
    }

    /// <summary>
    /// Returns the path to the RobloxPlayerBeta.exe for a specific version hash,
    /// searching both managed and standard folders.
    /// </summary>
    public static string? FindVersionPath(string versionHash)
    {
        // Check managed folder first
        string managed = Path.Combine(VersionsDir, versionHash, "RobloxPlayerBeta.exe");
        if (File.Exists(managed)) return managed;

        // Check standard Roblox folders
        string[] standardRoots =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions"),
            @"C:\Program Files (x86)\Roblox\Versions",
            @"C:\Program Files\Roblox\Versions",
        };

        foreach (string root in standardRoots)
        {
            string candidate = Path.Combine(root, versionHash, "RobloxPlayerBeta.exe");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>
    /// Returns the path to the RobloxPlayerBeta.exe in a custom folder.
    /// </summary>
    public static string? FindPlayerInFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return null;

        // Direct exe in folder
        string direct = Path.Combine(folderPath, "RobloxPlayerBeta.exe");
        if (File.Exists(direct)) return direct;

        // Search one level deep (version subfolders)
        foreach (var sub in Directory.GetDirectories(folderPath))
        {
            string candidate = Path.Combine(sub, "RobloxPlayerBeta.exe");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>
    /// Downloads and extracts the Roblox player for the given version hash.
    /// Reports progress as 0.0 to 1.0.
    /// </summary>
    public static async Task<string> DownloadVersionAsync(string versionHash, IProgress<(double progress, string status)>? progress = null)
    {
        string destDir = Path.Combine(VersionsDir, versionHash);
        Directory.CreateDirectory(destDir);

        progress?.Report((0.0, "Fetching package manifest..."));

        // Get package manifest
        string manifestUrl = $"https://setup.rbxcdn.com/{versionHash}-rbxPkgManifest.txt";
        string manifest;
        try
        {
            manifest = await _http.GetStringAsync(manifestUrl);
        }
        catch
        {
            // Fallback: try downloading the single zip
            progress?.Report((0.05, "Downloading RobloxApp.zip..."));
            await DownloadSinglePackageAsync(versionHash, destDir, progress);
            return Path.Combine(destDir, "RobloxPlayerBeta.exe");
        }

        // Parse manifest — format: groups of 4 lines (name, checksum, compressed_size, uncompressed_size)
        var lines = manifest.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var packages = new List<(string name, long size)>();

        for (int i = 0; i < lines.Length; i += 4)
        {
            if (i + 2 >= lines.Length) break;
            string pkgName = lines[i];
            if (long.TryParse(lines[i + 2], out long compressedSize))
                packages.Add((pkgName, compressedSize));
            else
                packages.Add((pkgName, 0));
        }

        if (packages.Count == 0)
        {
            // Fallback to single package
            await DownloadSinglePackageAsync(versionHash, destDir, progress);
            return Path.Combine(destDir, "RobloxPlayerBeta.exe");
        }

        long totalSize = packages.Sum(p => p.size);
        long downloadedTotal = 0;

        for (int i = 0; i < packages.Count; i++)
        {
            var (pkgName, pkgSize) = packages[i];
            string pkgUrl = $"https://setup.rbxcdn.com/{versionHash}-{pkgName}";

            progress?.Report(((double)downloadedTotal / Math.Max(totalSize, 1),
                $"Downloading {pkgName} ({i + 1}/{packages.Count})..."));

            try
            {
                string tempZip = Path.Combine(destDir, pkgName);
                await DownloadFileAsync(pkgUrl, tempZip, (bytesRead) =>
                {
                    double p = (double)(downloadedTotal + bytesRead) / Math.Max(totalSize, 1);
                    progress?.Report((Math.Min(p, 0.95), $"Downloading {pkgName} ({i + 1}/{packages.Count})..."));
                });

                // Extract zip
                progress?.Report(((double)(downloadedTotal + pkgSize) / Math.Max(totalSize, 1),
                    $"Extracting {pkgName}..."));

                if (pkgName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(tempZip, destDir, overwriteFiles: true);
                    }
                    catch { }
                    try { File.Delete(tempZip); } catch { }
                }

                downloadedTotal += pkgSize;
            }
            catch (Exception ex)
            {
                // Log but continue — some packages may be optional
                System.Diagnostics.Debug.WriteLine($"Failed to download {pkgName}: {ex.Message}");
            }
        }

        progress?.Report((1.0, "Installation complete!"));

        string playerPath = Path.Combine(destDir, "RobloxPlayerBeta.exe");
        if (!File.Exists(playerPath))
            throw new Exception($"Download completed but RobloxPlayerBeta.exe not found in {destDir}");

        return playerPath;
    }

    private static async Task DownloadSinglePackageAsync(string versionHash, string destDir, IProgress<(double progress, string status)>? progress)
    {
        string zipUrl = $"https://setup.rbxcdn.com/{versionHash}-RobloxApp.zip";
        string tempZip = Path.Combine(destDir, "RobloxApp.zip");

        await DownloadFileAsync(zipUrl, tempZip, (bytesRead) =>
        {
            progress?.Report((0.5, "Downloading Roblox..."));
        });

        progress?.Report((0.8, "Extracting..."));
        ZipFile.ExtractToDirectory(tempZip, destDir, overwriteFiles: true);
        try { File.Delete(tempZip); } catch { }
    }

    private static async Task DownloadFileAsync(string url, string destPath, Action<long>? onBytesRead = null)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var file = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;
            onBytesRead?.Invoke(totalRead);
        }
    }

    /// <summary>
    /// Ensures the latest version is available locally. Downloads if needed.
    /// Returns the path to RobloxPlayerBeta.exe.
    /// </summary>
    public static async Task<string> EnsureLatestVersionAsync(IProgress<(double progress, string status)>? progress = null)
    {
        progress?.Report((0.0, "Checking latest Roblox version..."));

        var versionInfo = await GetLatestVersionAsync();
        if (versionInfo == null)
            throw new Exception("Failed to fetch latest Roblox version. Check your internet connection.");

        string hash = versionInfo.clientVersionUpload;
        if (string.IsNullOrEmpty(hash))
            throw new Exception("Roblox returned an empty version hash.");

        // Check if already installed
        string? existingPath = FindVersionPath(hash);
        if (existingPath != null)
        {
            progress?.Report((1.0, $"Roblox {versionInfo.version} is up to date."));
            return existingPath;
        }

        // Download it
        progress?.Report((0.02, $"Downloading Roblox {versionInfo.version} ({hash})..."));
        return await DownloadVersionAsync(hash, progress);
    }
}
