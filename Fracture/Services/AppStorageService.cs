using System.IO;
using System.Text.Json;
using Fracture.Models;

namespace Fracture.Services;

public static class AppStorageService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fracture");
    private static readonly string FilePath = Path.Combine(DataDir, "apps.json");

    public static List<AppEntry> Load()
    {
        if (!File.Exists(FilePath))
            return new List<AppEntry>();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<AppEntry>>(json) ?? new List<AppEntry>();
        }
        catch
        {
            return new List<AppEntry>();
        }
    }

    public static void Save(List<AppEntry> apps)
    {
        Directory.CreateDirectory(DataDir);
        string json = JsonSerializer.Serialize(apps, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
