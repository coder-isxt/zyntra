using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Fracture.Models;

namespace Fracture.Services;

public static class ActivityLogService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fracture");
    private static readonly string FilePath = Path.Combine(DataDir, "activity_log.json");
    private const int MaxEntries = 500;

    private static bool _loaded;

    public static ObservableCollection<ActivityLogEntry> Entries { get; } = new();

    public static void Load()
    {
        if (_loaded) return;
        _loaded = true;

        Entries.Clear();
        if (!File.Exists(FilePath)) return;
        try
        {
            string json = File.ReadAllText(FilePath);
            var list = JsonSerializer.Deserialize<List<ActivityLogEntry>>(json) ?? new();
            foreach (var e in list.OrderByDescending(x => x.Timestamp))
                Entries.Add(e);
        }
        catch { }
    }

    public static void Log(ActivityKind kind, string message, string? detail = null)
    {
        Load();

        var entry = new ActivityLogEntry { Kind = kind, Message = message, Detail = detail };

        void Insert()
        {
            Entries.Insert(0, entry);
            while (Entries.Count > MaxEntries)
                Entries.RemoveAt(Entries.Count - 1);
            Save();
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            dispatcher.Invoke(Insert);
        else
            Insert();
    }

    public static void Clear()
    {
        Entries.Clear();
        Save();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            string json = JsonSerializer.Serialize(Entries.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { }
    }
}
