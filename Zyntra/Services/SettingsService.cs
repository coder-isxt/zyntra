using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace Zyntra.Services;

public class AppSettings
{
    public bool LaunchOnStartup { get; set; }
    public bool MinimizeToTray { get; set; }
    public string AccentColorHex { get; set; } = "#FF709BFF";
    public string DefaultPage { get; set; } = "Roblox";
    public bool DisableAnimations { get; set; }
    public bool AutoRefreshCookies { get; set; }
    public string DefaultTag { get; set; } = string.Empty;
    public bool HideInvalidAccounts { get; set; }
    public string DefaultScriptTemplate { get; set; } = "-- Your script here\nzyntra.log(\"Hello from Zyntra!\")\n\nfor _, acc in ipairs(zyntra.get_accounts()) do\n    zyntra.log(acc.DisplayName)\nend";
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public bool ShowSidebarBadges { get; set; } = true;
}

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Zyntra", "settings.json");

    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Zyntra";

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    public static void SetLaunchOnStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enabled)
            {
                string exePath = Environment.ProcessPath ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch { }
    }
}
