using System.IO;
using System.Text.Json;

namespace Zyntra.Services;

public class ScriptContext
{
    public string Version { get; set; } = string.Empty;
    public string DataDir { get; set; } = string.Empty;
    public string ResponseFile { get; set; } = string.Empty;
    public List<ScriptContextAccount> Accounts { get; set; } = new();
    public List<ScriptContextApp> Apps { get; set; } = new();
}

public class ScriptContextAccount
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public bool? CookieValid { get; set; }
}

public class ScriptContextApp
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGameModule { get; set; }
}

public class ScriptResponse
{
    public List<ScriptResponseNotification>? Notifications { get; set; }
    public string? SetClipboard { get; set; }
}

public class ScriptResponseNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, Success, Warning, Error
}

public static class ScriptContextService
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "Zyntra");

    public static string ExportContext()
    {
        Directory.CreateDirectory(TempDir);
        string contextPath = Path.Combine(TempDir, "zyntra_context.json");
        string responsePath = Path.Combine(TempDir, "zyntra_response.json");

        // Delete old response
        if (File.Exists(responsePath))
            File.Delete(responsePath);

        var accounts = AccountStorageService.Load();
        var apps = AppStorageService.Load();

        var context = new ScriptContext
        {
            Version = UpdateService.CurrentVersion,
            DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra"),
            ResponseFile = responsePath,
            Accounts = accounts.Select(a => new ScriptContextAccount
            {
                UserId = a.UserId.ToString(),
                Username = a.Username,
                DisplayName = a.DisplayName,
                Tag = a.Tag,
                CookieValid = a.CookieValid,
            }).ToList(),
            Apps = apps.Select(a => new ScriptContextApp
            {
                Id = a.Id,
                Name = a.Name,
                ExePath = a.ExePath,
                Description = a.Description,
                IsGameModule = a.IsGameModule,
            }).ToList(),
        };

        string json = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(contextPath, json);

        return contextPath;
    }

    public static void ProcessResponse()
    {
        string responsePath = Path.Combine(TempDir, "zyntra_response.json");
        if (!File.Exists(responsePath)) return;

        try
        {
            string json = File.ReadAllText(responsePath);
            var response = JsonSerializer.Deserialize<ScriptResponse>(json);
            if (response == null) return;

            if (response.Notifications != null)
            {
                foreach (var n in response.Notifications)
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
            }

            if (!string.IsNullOrEmpty(response.SetClipboard))
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    System.Windows.Clipboard.SetText(response.SetClipboard));
            }
        }
        catch { }
        finally
        {
            try { File.Delete(responsePath); } catch { }
        }
    }

    public static string GetApiDir()
    {
        string apiDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zyntra", "api");
        Directory.CreateDirectory(apiDir);
        return apiDir;
    }
}
