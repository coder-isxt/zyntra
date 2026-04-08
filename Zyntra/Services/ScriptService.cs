using System.Diagnostics;
using System.IO;
using System.Text.Json;
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
        string tempFile;
        ProcessStartInfo psi;

        switch (script.ScriptType)
        {
            case "Batch":
                tempFile = Path.Combine(Path.GetTempPath(), $"zyntra_{script.Id}.bat");
                File.WriteAllText(tempFile, script.Content);
                psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                break;

            case "Python":
                tempFile = Path.Combine(Path.GetTempPath(), $"zyntra_{script.Id}.py");
                File.WriteAllText(tempFile, script.Content);
                psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                break;

            default: // PowerShell
                tempFile = Path.Combine(Path.GetTempPath(), $"zyntra_{script.Id}.ps1");
                File.WriteAllText(tempFile, script.Content);
                psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                break;
        }

        try
        {
            using var process = Process.Start(psi);
            if (process == null) return "Failed to start process.";

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            script.LastRunAt = DateTime.UtcNow;

            return string.IsNullOrEmpty(error) ? output : $"{output}\n[STDERR]\n{error}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
