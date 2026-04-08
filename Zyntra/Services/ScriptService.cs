using System.Diagnostics;
using System.IO;
using System.Reflection;
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
        // Export context + extract API modules
        string contextPath = ScriptContextService.ExportContext();
        string apiDir = ScriptContextService.GetApiDir();
        ExtractApiModules(apiDir);

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
            {
                // Prepend API import
                string apiImport = $"import sys; sys.path.insert(0, r'{apiDir}')\nimport zyntra_api as zyntra\n\n";
                tempFile = Path.Combine(Path.GetTempPath(), $"zyntra_{script.Id}.py");
                File.WriteAllText(tempFile, apiImport + script.Content);
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
            }

            default: // PowerShell
            {
                string apiModule = Path.Combine(apiDir, "ZyntraAPI.psm1");
                // Prepend module import
                string psImport = $"Import-Module '{apiModule}' -Force\n\n";
                tempFile = Path.Combine(Path.GetTempPath(), $"zyntra_{script.Id}.ps1");
                File.WriteAllText(tempFile, psImport + script.Content);
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
        }

        // Set ZYNTRA_CONTEXT env var
        psi.EnvironmentVariables["ZYNTRA_CONTEXT"] = contextPath;

        try
        {
            using var process = Process.Start(psi);
            if (process == null) return "Failed to start process.";

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            script.LastRunAt = DateTime.UtcNow;

            // Process response file (notifications, clipboard, etc.)
            ScriptContextService.ProcessResponse();

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

    private static void ExtractApiModules(string apiDir)
    {
        ExtractResource("Zyntra.Resources.ZyntraAPI.psm1", Path.Combine(apiDir, "ZyntraAPI.psm1"));
        ExtractResource("Zyntra.Resources.zyntra_api.py", Path.Combine(apiDir, "zyntra_api.py"));
    }

    private static void ExtractResource(string resourceName, string outputPath)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) return;

            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fs);
        }
        catch { }
    }
}
