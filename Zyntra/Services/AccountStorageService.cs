using System.IO;
using System.Text.Json;
using Zyntra.Models;

namespace Zyntra.Services;

public static class AccountStorageService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");
    private static readonly string FilePath = Path.Combine(DataDir, "accounts.json");

    public static List<RobloxAccount> Load()
    {
        if (!File.Exists(FilePath))
            return new List<RobloxAccount>();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<RobloxAccount>>(json) ?? new List<RobloxAccount>();
        }
        catch
        {
            return new List<RobloxAccount>();
        }
    }

    public static void Save(List<RobloxAccount> accounts)
    {
        Directory.CreateDirectory(DataDir);
        string json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
