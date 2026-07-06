using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Fracture.Services;

public static class AdminService
{
    /// <summary>True when the current process is running with administrator rights.</summary>
    public static bool IsElevated
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Prompts the user, and if they agree relaunches Fracture elevated (UAC) and shuts
    /// down the current instance. Returns true if a relaunch was started.
    /// </summary>
    public static bool PromptRelaunchAsAdmin(string reason)
    {
        if (IsElevated) return false;

        var result = MessageBox.Show(
            $"{reason}\n\nRestart Fracture as administrator now?",
            "Administrator required", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return false;

        return RelaunchAsAdmin();
    }

    public static bool RelaunchAsAdmin()
    {
        try
        {
            string? exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe))
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Verb = "runas",
            };

            Process.Start(psi);
            Application.Current.Shutdown();
            return true;
        }
        catch
        {
            // User declined the UAC prompt or elevation failed.
            return false;
        }
    }
}
