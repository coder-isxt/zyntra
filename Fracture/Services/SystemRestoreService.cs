using System.Diagnostics;

namespace Fracture.Services;

/// <summary>
/// Creates Windows System Restore points before running destructive optimizations.
/// Requires administrator rights and System Protection enabled on the system drive.
/// </summary>
public static class SystemRestoreService
{
    public static Task<string> CreateRestorePointAsync(string description) => Task.Run(() =>
    {
        try
        {
            // Checkpoint-Computer is the simplest reliable way to make a restore point.
            string script = $"Checkpoint-Computer -Description \"{description}\" -RestorePointType MODIFY_SETTINGS";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                return "Could not start PowerShell to create a restore point";

            string err = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode == 0)
                return "Created a system restore point";

            if (err.Contains("1440", StringComparison.OrdinalIgnoreCase) ||
                err.Contains("frequency", StringComparison.OrdinalIgnoreCase))
                return "Skipped restore point (one was already created in the last 24h)";

            if (err.Contains("disabled", StringComparison.OrdinalIgnoreCase))
                return "Skipped restore point (System Protection is disabled)";

            return "Could not create a restore point (continuing anyway)";
        }
        catch (Exception ex)
        {
            return $"Restore point failed: {ex.Message}";
        }
    });
}
