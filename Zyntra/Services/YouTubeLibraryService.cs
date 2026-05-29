using System.IO;
using System.Text.Json;
using Zyntra.Models;

namespace Zyntra.Services;

public static class YouTubeLibraryService
{
    private static readonly object SaveLock = new();
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");
    private static readonly string FilePath = Path.Combine(DataDir, "youtube_library.json");

    public static YouTubeLibraryData Load()
    {
        if (!File.Exists(FilePath))
            return new YouTubeLibraryData();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<YouTubeLibraryData>(json) ?? new YouTubeLibraryData();
        }
        catch
        {
            return new YouTubeLibraryData();
        }
    }

    public static void Save(YouTubeLibraryData library)
    {
        lock (SaveLock)
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                string json = JsonSerializer.Serialize(library, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Ignore transient IO errors during rapid progress saves.
            }
        }
    }
}
