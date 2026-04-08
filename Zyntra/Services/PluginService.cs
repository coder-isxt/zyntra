using System.IO;
using System.Reflection;
using System.Text.Json;
using Zyntra.Models;

namespace Zyntra.Services;

public interface IZyntraPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    void Initialize();
    void Execute();
    void Shutdown();
}

public static class PluginService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");
    private static readonly string PluginsDir = Path.Combine(DataDir, "plugins");
    private static readonly string IndexPath = Path.Combine(DataDir, "plugins.json");

    private static readonly List<(PluginEntry Entry, IZyntraPlugin? Instance)> _loaded = new();

    public static List<PluginEntry> LoadIndex()
    {
        if (!File.Exists(IndexPath))
            return new List<PluginEntry>();
        try
        {
            string json = File.ReadAllText(IndexPath);
            return JsonSerializer.Deserialize<List<PluginEntry>>(json) ?? new List<PluginEntry>();
        }
        catch { return new List<PluginEntry>(); }
    }

    public static void SaveIndex(List<PluginEntry> plugins)
    {
        Directory.CreateDirectory(DataDir);
        string json = JsonSerializer.Serialize(plugins, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(IndexPath, json);
    }

    public static PluginEntry? InstallPlugin(string dllPath)
    {
        Directory.CreateDirectory(PluginsDir);
        string destName = Path.GetFileName(dllPath);
        string destPath = Path.Combine(PluginsDir, destName);

        try
        {
            File.Copy(dllPath, destPath, true);

            // Try to load metadata
            var asm = Assembly.LoadFrom(destPath);
            var pluginType = asm.GetTypes().FirstOrDefault(t =>
                typeof(IZyntraPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            var entry = new PluginEntry { DllPath = destPath };

            if (pluginType != null)
            {
                var instance = Activator.CreateInstance(pluginType) as IZyntraPlugin;
                if (instance != null)
                {
                    entry.Name = instance.Name;
                    entry.Description = instance.Description;
                    entry.Version = instance.Version;
                }
            }

            if (string.IsNullOrEmpty(entry.Name))
                entry.Name = Path.GetFileNameWithoutExtension(dllPath);

            return entry;
        }
        catch
        {
            return null;
        }
    }

    public static void LoadAndRunEnabled(List<PluginEntry> plugins)
    {
        foreach (var entry in plugins.Where(p => p.IsEnabled))
        {
            try
            {
                if (!File.Exists(entry.DllPath)) continue;

                var asm = Assembly.LoadFrom(entry.DllPath);
                var pluginType = asm.GetTypes().FirstOrDefault(t =>
                    typeof(IZyntraPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (pluginType != null)
                {
                    var instance = Activator.CreateInstance(pluginType) as IZyntraPlugin;
                    instance?.Initialize();
                    _loaded.Add((entry, instance));
                }
            }
            catch
            {
                NotificationService.Push("Plugin Error", $"Failed to load {entry.Name}", NotificationType.Error);
            }
        }
    }

    public static void ShutdownAll()
    {
        foreach (var (_, instance) in _loaded)
        {
            try { instance?.Shutdown(); } catch { }
        }
        _loaded.Clear();
    }
}
