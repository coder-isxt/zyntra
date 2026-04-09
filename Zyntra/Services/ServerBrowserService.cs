using System.IO;
using System.Text.Json;
using Zyntra.Models;

namespace Zyntra.Services;

public static class ServerBrowserService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");
    private static readonly string FavoritesPath = Path.Combine(DataDir, "favorites.json");

    public static List<ServerEntry> LoadFavorites()
    {
        if (!File.Exists(FavoritesPath))
            return new List<ServerEntry>();
        try
        {
            string json = File.ReadAllText(FavoritesPath);
            return JsonSerializer.Deserialize<List<ServerEntry>>(json) ?? new List<ServerEntry>();
        }
        catch { return new List<ServerEntry>(); }
    }

    public static void SaveFavorites(List<ServerEntry> favorites)
    {
        Directory.CreateDirectory(DataDir);
        string json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FavoritesPath, json);
    }

    public static void AddFavorite(ServerEntry entry)
    {
        entry.IsFavorite = true;
        var list = LoadFavorites();
        list.Add(entry);
        SaveFavorites(list);
    }

    public static void RemoveFavorite(string id)
    {
        var list = LoadFavorites();
        list.RemoveAll(f => f.Id == id);
        SaveFavorites(list);
    }
}
