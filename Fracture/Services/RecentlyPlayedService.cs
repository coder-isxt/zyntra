using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Fracture.Models;

namespace Fracture.Services;

public static class RecentlyPlayedService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fracture");
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
            // Step 1: Get universe ID from place ID
            var uniResponse = await _http.GetAsync($"https://apis.roblox.com/universes/v1/places/{placeId}/universe");
            if (!uniResponse.IsSuccessStatusCode)
                return $"Place {placeId}";

            string uniJson = await uniResponse.Content.ReadAsStringAsync();
            using var uniDoc = JsonDocument.Parse(uniJson);
            if (!uniDoc.RootElement.TryGetProperty("universeId", out var universeIdProp))
                return $"Place {placeId}";
            long universeId = universeIdProp.GetInt64();

            // Step 2: Get game name from universe ID
            var gameResponse = await _http.GetAsync($"https://games.roblox.com/v1/games?universeIds={universeId}");
            if (!gameResponse.IsSuccessStatusCode)
                return $"Place {placeId}";

            string gameJson = await gameResponse.Content.ReadAsStringAsync();
            using var gameDoc = JsonDocument.Parse(gameJson);
            if (gameDoc.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
            {
                var item = data[0];
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
