using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Fracture.Models;

namespace Fracture.Services;

public static class FavoriteGamesService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fracture");
    private static readonly string FilePath = Path.Combine(DataDir, "favorite_games.json");

    public static ObservableCollection<FavoriteGame> Games { get; } = new();

    public static void Load()
    {
        Games.Clear();
        if (!File.Exists(FilePath)) return;
        try
        {
            string json = File.ReadAllText(FilePath);
            var list = JsonSerializer.Deserialize<List<FavoriteGame>>(json) ?? new();
            foreach (var g in list.OrderBy(x => x.GameName))
                Games.Add(g);
        }
        catch { }
    }

    public static async Task AddAsync(long placeId)
    {
        if (Games.Any(g => g.PlaceId == placeId)) return;

        string name = await RecentlyPlayedService.ResolveGameNameAsync(placeId);
        var entry = new FavoriteGame { PlaceId = placeId, GameName = name };

        System.Windows.Application.Current?.Dispatcher.Invoke(() => Games.Add(entry));
        Save();
    }

    public static void Remove(string id)
    {
        var game = Games.FirstOrDefault(g => g.Id == id);
        if (game != null)
        {
            Games.Remove(game);
            Save();
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var list = Games.ToList();
            string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { }
    }
}
