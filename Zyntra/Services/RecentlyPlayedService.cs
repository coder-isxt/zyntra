using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Zyntra.Models;

namespace Zyntra.Services;

public static class RecentlyPlayedService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");
    private static readonly string FilePath = Path.Combine(DataDir, "recent_games.json");
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static ObservableCollection<RecentGame> Games { get; } = new();

    public static void Load()
    {
        Games.Clear();
        if (!File.Exists(FilePath)) return;
        try
        {
            string json = File.ReadAllText(FilePath);
            var list = JsonSerializer.Deserialize<List<RecentGame>>(json) ?? new();
            foreach (var g in list.OrderByDescending(x => x.PlayedAt).Take(20))
                Games.Add(g);
        }
        catch { }
    }

    public static async Task AddGameAsync(long placeId, string? accountName)
    {
        string gameName = await ResolveGameNameAsync(placeId);

        var entry = new RecentGame
        {
            PlaceId = placeId,
            GameName = gameName,
            AccountName = accountName,
            PlayedAt = DateTime.Now,
        };

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            // Remove old entry for same place+account if exists
            var existing = Games.FirstOrDefault(g => g.PlaceId == placeId && g.AccountName == accountName);
            if (existing != null) Games.Remove(existing);

            Games.Insert(0, entry);
            while (Games.Count > 20)
                Games.RemoveAt(Games.Count - 1);
        });

        Save();
    }

    public static async Task<string> ResolveGameNameAsync(long placeId)
    {
        try
        {
            string url = $"https://games.roblox.com/v1/games/multiget-place-details?placeIds={placeId}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return $"Place {placeId}";

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.GetArrayLength() > 0)
            {
                var item = root[0];
                if (item.TryGetProperty("name", out var nameProp))
                    return nameProp.GetString() ?? $"Place {placeId}";
            }
        }
        catch { }
        return $"Place {placeId}";
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

    public static void Clear()
    {
        Games.Clear();
        Save();
    }
}
