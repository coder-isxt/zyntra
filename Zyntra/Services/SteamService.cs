using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Zyntra.Services;

public static class SteamService
{
    private const string SteamRegistryKey = @"SOFTWARE\Valve\Steam";

    public static string? GetSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SteamRegistryKey);
            return key?.GetValue("SteamPath") as string;
        }
        catch { return null; }
    }

    public static string? GetCurrentUser()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SteamRegistryKey);
            return key?.GetValue("AutoLoginUser") as string;
        }
        catch { return null; }
    }

    public static List<string> GetKnownUsers()
    {
        var users = new List<string>();
        try
        {
            string? steamPath = GetSteamPath();
            if (steamPath == null) return users;

            string configPath = Path.Combine(steamPath, "config", "loginusers.vdf");
            if (!File.Exists(configPath)) return users;

            string content = File.ReadAllText(configPath);
            // Simple VDF parse for "PersonaName" entries
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Contains("\"AccountName\""))
                {
                    string name = ExtractVdfValue(line);
                    if (!string.IsNullOrEmpty(name))
                        users.Add(name);
                }
            }
        }
        catch { }
        return users;
    }

    public static void SwitchAccount(string username)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SteamRegistryKey, true);
            if (key == null) return;

            key.SetValue("AutoLoginUser", username);
            key.SetValue("RememberPassword", 1);
        }
        catch { }
    }

    public static void RestartSteam(string? username = null)
    {
        if (username != null)
            SwitchAccount(username);

        string? steamPath = GetSteamPath();
        if (steamPath == null) return;

        string steamExe = Path.Combine(steamPath, "steam.exe");
        if (!File.Exists(steamExe)) return;

        // Kill Steam
        try
        {
            foreach (var proc in Process.GetProcessesByName("steam"))
                proc.Kill();
        }
        catch { }

        // Wait for Steam to close
        Task.Delay(2000).Wait();

        // Restart
        Process.Start(new ProcessStartInfo
        {
            FileName = steamExe,
            UseShellExecute = true,
        });
    }

    public static void LaunchGame(long appId, string? username = null)
    {
        if (username != null)
            SwitchAccount(username);

        Process.Start(new ProcessStartInfo
        {
            FileName = $"steam://rungameid/{appId}",
            UseShellExecute = true,
        });
    }

    private static string ExtractVdfValue(string line)
    {
        var parts = line.Split('"');
        return parts.Length >= 4 ? parts[3] : string.Empty;
    }
}
