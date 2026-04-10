using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using MoonSharp.Interpreter;
using Zyntra.Models;

namespace Zyntra.Services;

public static class ScriptService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");
    private static readonly string FilePath = Path.Combine(DataDir, "scripts.json");

    public static List<ScriptEntry> Load()
    {
        if (!File.Exists(FilePath))
            return new List<ScriptEntry>();
        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<ScriptEntry>>(json) ?? new List<ScriptEntry>();
        }
        catch { return new List<ScriptEntry>(); }
    }

    public static void Save(List<ScriptEntry> scripts)
    {
        Directory.CreateDirectory(DataDir);
        string json = JsonSerializer.Serialize(scripts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    public static async Task<string> RunAsync(ScriptEntry script)
    {
        return await Task.Run(() =>
        {
            var output = new StringBuilder();
            var notifications = new List<ScriptResponseNotification>();
            string? clipboard = null;
            var launches = new List<ScriptResponseLaunch>();

            try
            {
                var luaScript = new Script(CoreModules.Preset_SoftSandbox | CoreModules.Metatables |
                                           CoreModules.String | CoreModules.Table | CoreModules.Math |
                                           CoreModules.OS_Time);

                // Build context table
                var context = BuildContextTable(luaScript);
                luaScript.Globals["_zyntra_context"] = context;

                // Register C# callbacks
                luaScript.Globals["_zyntra_log"] = (Action<string>)(msg =>
                {
                    string ts = DateTime.Now.ToString("HH:mm:ss");
                    output.AppendLine($"[{ts}] {msg}");
                });

                luaScript.Globals["_zyntra_notify"] = (Action<string, string, string>)((title, message, type) =>
                {
                    notifications.Add(new ScriptResponseNotification
                    {
                        Title = title, Message = message, Type = type
                    });
                });

                luaScript.Globals["_zyntra_set_clipboard"] = (Action<string>)(text =>
                {
                    clipboard = text;
                });

                luaScript.Globals["_zyntra_launch_game"] = (Action<string, long>)((accountName, placeId) =>
                {
                    launches.Add(new ScriptResponseLaunch
                    {
                        AccountName = accountName, PlaceId = placeId
                    });
                });

                luaScript.Globals["_zyntra_sleep"] = (Action<int>)(ms =>
                {
                    Thread.Sleep(ms);
                });

                // Override print to capture output
                luaScript.Globals["print"] = (Action<DynValue[]>)(args =>
                {
                    var line = string.Join("\t", args.Select(a => a.ToPrintString()));
                    output.AppendLine(line);
                });

                // Load the Lua API module
                string apiLua = LoadEmbeddedLuaApi();
                luaScript.DoString(apiLua, null, "zyntra_api");

                // Execute user script
                luaScript.DoString(script.Content, null, script.Name);
                script.LastRunAt = DateTime.UtcNow;
            }
            catch (InterpreterException ex)
            {
                output.AppendLine($"[ERROR] {ex.DecoratedMessage}");
            }
            catch (Exception ex)
            {
                output.AppendLine($"[ERROR] {ex.Message}");
            }

            // Process actions
            foreach (var n in notifications)
            {
                var type = n.Type?.ToLower() switch
                {
                    "success" => NotificationType.Success,
                    "warning" => NotificationType.Warning,
                    "error" => NotificationType.Error,
                    _ => NotificationType.Info,
                };
                NotificationService.Push(n.Title, n.Message, type);
            }

            if (!string.IsNullOrEmpty(clipboard))
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    System.Windows.Clipboard.SetText(clipboard));
            }

            foreach (var launch in launches)
            {
                _ = ProcessLaunchAsync(launch);
            }

            return output.ToString();
        });
    }

    private static Table BuildContextTable(Script luaScript)
    {
        var accounts = AccountStorageService.Load();
        var apps = AppStorageService.Load();
        var recentGames = RecentlyPlayedService.Games.ToList();

        var ctx = new Table(luaScript);
        ctx["Version"] = UpdateService.CurrentVersion;
        ctx["DataDir"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra");

        // Accounts
        var accsTable = new Table(luaScript);
        for (int i = 0; i < accounts.Count; i++)
        {
            var a = accounts[i];
            var t = new Table(luaScript);
            t["UserId"] = a.UserId.ToString();
            t["Username"] = a.Username;
            t["DisplayName"] = a.DisplayName;
            t["Tag"] = a.Tag ?? "";
            t["CookieValid"] = a.CookieValid ?? false;
            accsTable[i + 1] = t;
        }
        ctx["Accounts"] = accsTable;

        // Apps
        var appsTable = new Table(luaScript);
        for (int i = 0; i < apps.Count; i++)
        {
            var a = apps[i];
            var t = new Table(luaScript);
            t["Id"] = a.Id;
            t["Name"] = a.Name;
            t["ExePath"] = a.ExePath;
            t["Description"] = a.Description ?? "";
            t["IsGameModule"] = a.IsGameModule;
            appsTable[i + 1] = t;
        }
        ctx["Apps"] = appsTable;

        // Recent Games
        var gamesTable = new Table(luaScript);
        for (int i = 0; i < recentGames.Count; i++)
        {
            var g = recentGames[i];
            var t = new Table(luaScript);
            t["PlaceId"] = (double)g.PlaceId;
            t["GameName"] = g.GameName;
            t["AccountName"] = g.AccountName ?? "";
            t["PlayedAt"] = g.PlayedAt.ToString("yyyy-MM-dd HH:mm:ss");
            gamesTable[i + 1] = t;
        }
        ctx["RecentGames"] = gamesTable;

        return ctx;
    }

    private static string LoadEmbeddedLuaApi()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Zyntra.Resources.zyntra_api.lua");
        if (stream == null) return "";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task ProcessLaunchAsync(ScriptResponseLaunch launch)
    {
        try
        {
            var accounts = AccountStorageService.Load();
            var account = accounts.FirstOrDefault(a =>
                a.Username.Equals(launch.AccountName, StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Equals(launch.AccountName, StringComparison.OrdinalIgnoreCase));

            if (account == null)
            {
                NotificationService.Push("Launch Failed",
                    $"Account '{launch.AccountName}' not found.", NotificationType.Error);
                return;
            }

            string cookie = CryptoService.Decrypt(account.EncryptedCookie);
            await RobloxService.LaunchRobloxAsync(cookie, launch.PlaceId);
            await RecentlyPlayedService.AddGameAsync(launch.PlaceId, account.DisplayName);

            NotificationService.Push("Game Launched",
                $"Launched Place {launch.PlaceId} as {account.DisplayName}", NotificationType.Success);
        }
        catch (Exception ex)
        {
            NotificationService.Push("Launch Failed",
                $"Failed to launch Place {launch.PlaceId}: {ex.Message}", NotificationType.Error);
        }
    }
}
