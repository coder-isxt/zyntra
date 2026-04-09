using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Zyntra.Models;

namespace Zyntra.Services;

public static class ActivityLogService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");
    private static readonly string LogPath = Path.Combine(DataDir, "activity_log.json");

    public static ObservableCollection<ActivityLogEntry> Entries { get; } = new();

    public static void Load()
    {
        Entries.Clear();
        if (!File.Exists(LogPath)) return;
        try
        {
            string json = File.ReadAllText(LogPath);
            var list = JsonSerializer.Deserialize<List<ActivityLogEntry>>(json) ?? new();
            foreach (var e in list.OrderByDescending(x => x.Timestamp).Take(200))
                Entries.Add(e);
        }
        catch { }
    }

    public static void Log(string action, string details, string? accountName = null, long? placeId = null, string? placeName = null)
    {
        var entry = new ActivityLogEntry
        {
            Action = action,
            Details = details,
            AccountName = accountName,
            PlaceId = placeId,
            PlaceName = placeName,
        };

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Entries.Insert(0, entry);
            while (Entries.Count > 200)
                Entries.RemoveAt(Entries.Count - 1);
        });

        Save();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var list = Entries.ToList();
            string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LogPath, json);
        }
        catch { }
    }

    public static void Clear()
    {
        Entries.Clear();
        Save();
    }
}
