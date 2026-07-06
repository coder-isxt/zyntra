using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Fracture.Services;

/// <summary>
/// General Windows PC/laptop performance optimizations (not Roblox-specific).
/// Every method is defensive: it swallows per-item failures and returns a
/// human-readable summary of what it managed to do.
/// </summary>
public static class OptimizationService
{
    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("shell32.dll")]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SHERB_NOCONFIRMATION = 0x1;
    private const uint SHERB_NOPROGRESSUI = 0x2;
    private const uint SHERB_NOSOUND = 0x4;

    [DllImport("ntdll.dll")]
    private static extern uint NtSetSystemInformation(int infoClass, ref int info, int length);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string? host, string name, out long luid);

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        public long Luid;
        public int Attributes;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAll,
        ref TOKEN_PRIVILEGES newState, int len, IntPtr prev, IntPtr retLen);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    private const int SystemMemoryListInformation = 80;
    private const int MemoryPurgeStandbyList = 4;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
    private const uint TOKEN_QUERY = 0x8;
    private const int SE_PRIVILEGE_ENABLED = 0x2;

    /// <summary>Deletes files from the user and system TEMP folders.</summary>
    public static Task<string> ClearTempFilesAsync() => Task.Run(() =>
    {
        var folders = new[]
        {
            Path.GetTempPath(),
            Environment.GetEnvironmentVariable("TEMP") ?? string.Empty,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
        };

        long bytes = 0;
        int files = 0;
        foreach (var folder in folders.Where(f => !string.IsNullOrEmpty(f)).Distinct())
        {
            var (b, f2) = DeleteFolderContents(folder);
            bytes += b;
            files += f2;
        }

        return $"Removed {files} temp file(s), freed {FormatBytes(bytes)}";
    });

    /// <summary>Flushes the Windows DNS resolver cache.</summary>
    public static Task<string> FlushDnsAsync() => RunProcessAsync(
        "ipconfig", "/flushdns", "Flushed the DNS resolver cache");

    /// <summary>Switches Windows to the High Performance power plan.</summary>
    public static Task<string> SetHighPerformancePowerPlanAsync() => RunProcessAsync(
        "powercfg", "/setactive SCHEME_MIN", "Activated the High Performance power plan");

    /// <summary>Trims the working set of every accessible process to free standby RAM.</summary>
    public static Task<string> FreeMemoryAsync() => Task.Run(() =>
    {
        long freed = 0;
        int trimmed = 0;

        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                long before = proc.WorkingSet64;
                if (EmptyWorkingSet(proc.Handle))
                {
                    proc.Refresh();
                    long delta = before - proc.WorkingSet64;
                    if (delta > 0) freed += delta;
                    trimmed++;
                }
            }
            catch
            {
                // Access denied for protected/system processes — skip.
            }
            finally
            {
                proc.Dispose();
            }
        }

        return $"Trimmed {trimmed} process(es), freed ~{FormatBytes(freed)} of RAM";
    });

    /// <summary>Empties the Recycle Bin for all drives.</summary>
    public static Task<string> EmptyRecycleBinAsync() => Task.Run(() =>
    {
        try
        {
            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            return "Emptied the Recycle Bin";
        }
        catch (Exception ex)
        {
            return $"Could not empty Recycle Bin: {ex.Message}";
        }
    });

    // ---- Destructive (require confirmation / restore point) ----

    /// <summary>Deep clean: Windows Update cache, Prefetch, thumbnail cache, crash dumps. Requires admin.</summary>
    public static async Task<string> DeepCleanAsync()
    {
        long bytes = 0;
        int files = 0;

        // Stop Windows Update so its download cache can be cleared.
        await RunProcessAsync("net", "stop wuauserv", "");
        await RunProcessAsync("net", "stop bits", "");

        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        foreach (var folder in new[]
        {
            Path.Combine(windows, "SoftwareDistribution", "Download"),
            Path.Combine(windows, "Prefetch"),
            Path.Combine(localApp, "CrashDumps"),
        })
        {
            var (b, f) = DeleteFolderContents(folder);
            bytes += b; files += f;
        }

        // Thumbnail cache files (Explorer usually locks some — skip those).
        string explorer = Path.Combine(localApp, "Microsoft", "Windows", "Explorer");
        if (Directory.Exists(explorer))
        {
            foreach (var file in Directory.EnumerateFiles(explorer, "thumbcache_*.db"))
            {
                try { var i = new FileInfo(file); long s = i.Length; i.Delete(); bytes += s; files++; }
                catch { }
            }
        }

        await RunProcessAsync("net", "start wuauserv", "");
        await RunProcessAsync("net", "start bits", "");

        return $"Deep clean removed {files} file(s), freed {FormatBytes(bytes)}";
    }

    /// <summary>Closes common background/bloat apps that keep running in the tray.</summary>
    public static Task<string> CloseBackgroundAppsAsync() => Task.Run(() =>
    {
        // Curated list of well-known background helpers (no critical system processes).
        var targets = new[]
        {
            "OneDrive", "Teams", "msteams", "ms-teams", "Spotify", "Skype", "SkypeApp",
            "YourPhone", "PhoneExperienceHost", "GameBar", "GameBarFTServer",
            "Widgets", "WidgetService", "Cortana", "SearchApp"
        };

        int killed = 0;
        foreach (var name in targets)
        {
            foreach (var proc in Process.GetProcessesByName(name))
            {
                try { proc.Kill(); proc.WaitForExit(2000); killed++; }
                catch { }
                finally { proc.Dispose(); }
            }
        }

        return killed > 0
            ? $"Closed {killed} background app(s)"
            : "No known background apps were running";
    });

    /// <summary>Applies performance-oriented Windows tweaks (visual effects, Game DVR off).</summary>
    public static Task<string> ApplyPerformanceTweaksAsync() => Task.Run(() =>
    {
        var applied = new List<string>();

        try
        {
            using var vfx = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
            vfx.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord);
            applied.Add("visual effects → best performance");
        }
        catch { }

        try
        {
            using var dvr = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore");
            dvr.SetValue("GameDVR_Enabled", 0, RegistryValueKind.DWord);
            applied.Add("Game DVR disabled");
        }
        catch { }

        try
        {
            using var policy = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Policies\Microsoft\Windows\GameDVR");
            policy.SetValue("AllowGameDVR", 0, RegistryValueKind.DWord);
            applied.Add("Game DVR policy disabled");
        }
        catch { /* needs admin — skipped if not elevated */ }

        return applied.Count > 0
            ? $"Applied: {string.Join(", ", applied)}. Sign out to fully apply."
            : "No tweaks could be applied";
    });

    // ---- Admin-required ----

    /// <summary>Purges the Windows standby (cached) memory list. Requires admin.</summary>
    public static Task<string> PurgeStandbyMemoryAsync() => Task.Run(() =>
    {
        try
        {
            if (!EnablePrivilege("SeProfileSingleProcessPrivilege"))
                return "Could not acquire the required privilege (run as administrator)";

            int command = MemoryPurgeStandbyList;
            uint status = NtSetSystemInformation(SystemMemoryListInformation, ref command, sizeof(int));
            return status == 0
                ? "Purged the standby memory list"
                : $"Standby purge failed (status 0x{status:X})";
        }
        catch (Exception ex)
        {
            return $"Standby purge failed: {ex.Message}";
        }
    });

    /// <summary>Cleans up the Windows component store (WinSxS). Requires admin.</summary>
    public static Task<string> CleanComponentStoreAsync() => RunProcessAsync(
        "dism.exe", "/Online /Cleanup-Image /StartComponentCleanup",
        "Cleaned up the Windows component store (WinSxS)");

    /// <summary>Runs SFC and DISM to repair system files. Requires admin, can take a while.</summary>
    public static async Task<string> RepairSystemFilesAsync()
    {
        string sfc = await RunProcessAsync("sfc", "/scannow", "SFC completed");
        string dism = await RunProcessAsync("dism.exe",
            "/Online /Cleanup-Image /RestoreHealth", "DISM completed");
        return $"System file repair finished. {sfc}; {dism}";
    }

    /// <summary>Resets the Windows network stack. Requires admin and a reboot to take effect.</summary>
    public static async Task<string> ResetNetworkAsync()
    {
        await RunProcessAsync("netsh", "winsock reset", "");
        await RunProcessAsync("netsh", "int ip reset", "");
        await RunProcessAsync("ipconfig", "/flushdns", "");
        await RunProcessAsync("ipconfig", "/release", "");
        await RunProcessAsync("ipconfig", "/renew", "");
        return "Network stack reset — restart your PC to complete";
    }

    /// <summary>Stops and disables telemetry/diagnostic background services. Requires admin.</summary>
    public static async Task<string> StopTelemetryServicesAsync()
    {
        var services = new[] { "DiagTrack", "dmwappushservice" };
        int done = 0;
        foreach (var svc in services)
        {
            await RunProcessAsync("sc", $"stop {svc}", "");
            string r = await RunProcessAsync("sc", $"config {svc} start= disabled", "");
            if (!r.StartsWith("Failed")) done++;
        }
        return $"Stopped and disabled {done} telemetry service(s)";
    }

    private static bool EnablePrivilege(string name)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr token))
            return false;

        if (!LookupPrivilegeValue(null, name, out long luid))
            return false;

        var tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Luid = luid,
            Attributes = SE_PRIVILEGE_ENABLED,
        };

        return AdjustTokenPrivileges(token, false, ref tp, Marshal.SizeOf<TOKEN_PRIVILEGES>(), IntPtr.Zero, IntPtr.Zero)
               && Marshal.GetLastWin32Error() == 0;
    }

    private static async Task<string> RunProcessAsync(string fileName, string args, string successText)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                return $"Failed to start {fileName}";

            await proc.WaitForExitAsync();
            return proc.ExitCode == 0 ? successText : $"{fileName} exited with code {proc.ExitCode}";
        }
        catch (Exception ex)
        {
            return $"Failed: {ex.Message}";
        }
    }

    private static (long bytes, int files) DeleteFolderContents(string path)
    {
        long bytes = 0;
        int count = 0;

        if (!Directory.Exists(path))
            return (0, 0);

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(file);
                    long size = info.Length;
                    info.Delete();
                    bytes += size;
                    count++;
                }
                catch
                {
                    // File is locked / in use — skip.
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                try { Directory.Delete(dir, recursive: true); }
                catch { /* in use — skip */ }
            }
        }
        catch
        {
            // Access to the folder failed entirely — skip.
        }

        return (bytes, count);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 MB";
        double mb = bytes / (1024d * 1024d);
        if (mb < 1024) return $"{mb:0.#} MB";
        return $"{mb / 1024:0.##} GB";
    }
}
