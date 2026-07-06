using System.IO;
using System.Text.Json;

namespace Fracture.Services;

/// <summary>
/// Reads and writes Roblox FastFlags via the ClientAppSettings.json file located in
/// each Roblox version folder (ClientSettings/ClientAppSettings.json).
/// </summary>
public static class FastFlagService
{
    /// <summary>Returns every Roblox version directory that contains RobloxPlayerBeta.exe.</summary>
    public static List<string> GetVersionFolders()
    {
        var results = new List<string>();
        var settings = SettingsService.Load();

        var roots = new List<string>();
        if (!string.IsNullOrEmpty(settings.RobloxPlayerFolder))
        {
            // The configured folder may itself be a version folder or a parent "Versions" folder.
            roots.Add(settings.RobloxPlayerFolder);
            var parent = Directory.GetParent(settings.RobloxPlayerFolder)?.FullName;
            if (parent != null) roots.Add(parent);
        }

        roots.Add(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions"));
        roots.Add(@"C:\Program Files (x86)\Roblox\Versions");
        roots.Add(@"C:\Program Files\Roblox\Versions");

        foreach (string root in roots.Distinct())
        {
            if (!Directory.Exists(root)) continue;

            if (File.Exists(Path.Combine(root, "RobloxPlayerBeta.exe")))
                results.Add(root);

            try
            {
                foreach (string dir in Directory.GetDirectories(root))
                {
                    if (File.Exists(Path.Combine(dir, "RobloxPlayerBeta.exe")))
                        results.Add(dir);
                }
            }
            catch { }
        }

        return results.Distinct().ToList();
    }

    private static string? GetSettingsFile(string versionFolder)
        => Path.Combine(versionFolder, "ClientSettings", "ClientAppSettings.json");

    /// <summary>Loads existing FastFlags from the first version folder that has a settings file.</summary>
    public static Dictionary<string, string> Load()
    {
        foreach (string folder in GetVersionFolders())
        {
            string file = GetSettingsFile(folder)!;
            if (!File.Exists(file)) continue;

            try
            {
                string json = File.ReadAllText(file);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (dict != null)
                {
                    return dict.ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.ValueKind == JsonValueKind.String
                            ? kv.Value.GetString() ?? string.Empty
                            : kv.Value.ToString());
                }
            }
            catch { }
        }

        return new Dictionary<string, string>();
    }

    /// <summary>Writes the FastFlags to every detected Roblox version folder. Returns folders written.</summary>
    public static int Save(Dictionary<string, string> flags)
    {
        int written = 0;
        string json = JsonSerializer.Serialize(flags, new JsonSerializerOptions { WriteIndented = true });

        foreach (string folder in GetVersionFolders())
        {
            try
            {
                string dir = Path.Combine(folder, "ClientSettings");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "ClientAppSettings.json"), json);
                written++;
            }
            catch { }
        }

        return written;
    }

    /// <summary>Parses a FastFlag JSON string into a dictionary, throwing on invalid JSON.</summary>
    public static Dictionary<string, string> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>();

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? throw new Exception("FastFlags must be a JSON object.");

        return dict.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ValueKind == JsonValueKind.String
                ? kv.Value.GetString() ?? string.Empty
                : kv.Value.ToString());
    }

    public static string ToJson(Dictionary<string, string> flags)
        => JsonSerializer.Serialize(flags, new JsonSerializerOptions { WriteIndented = true });

    public record FastFlagPreset(string Key, string Value, string Description);

    /// <summary>Common, well-known FastFlags offered as quick toggles in the UI.</summary>
    public static readonly FastFlagPreset[] CommonPresets =
    {
        // Performance
        new("DFIntTaskSchedulerTargetFps", "144", "Unlock FPS cap (set target FPS)"),
        new("DFIntDebugFRMQualityLevelOverride", "1", "Lock render quality to lowest (max FPS)"),
        new("FFlagDisablePostFx", "True", "Disable post-processing effects"),
        new("FIntRenderShadowIntensity", "0", "Disable shadows"),
        new("DFFlagDebugPauseVoxelizer", "True", "Disable voxel (dynamic) shadows"),
        new("FIntDebugForceMSAASamples", "4", "Force anti-aliasing (MSAA samples: 0/1/2/4/8)"),

        // Renderer
        new("FFlagDebugGraphicsPreferD3D11", "True", "Force DirectX 11 rendering"),
        new("FFlagDebugGraphicsPreferD3D11FL10", "True", "Force DirectX 11 (feature level 10, old GPUs)"),
        new("FFlagDebugGraphicsPreferVulkan", "True", "Force Vulkan rendering"),
        new("FFlagDebugGraphicsPreferOpenGL", "True", "Force OpenGL rendering"),

        // Visuals
        new("FFlagDebugForceFutureIsBrightPhase3", "True", "Force Future lighting technology"),
        new("DFIntCameraFieldOfViewMaximum", "120", "Increase maximum field of view"),
        new("DFFlagDisableDPIScale", "True", "Disable DPI scaling"),

        // Network
        new("DFIntConnectionMTUSize", "900", "Lower network MTU (may reduce packet loss)"),
    };
}
