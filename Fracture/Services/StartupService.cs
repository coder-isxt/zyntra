using System.IO;
using Fracture.Models;
using Microsoft.Win32;

namespace Fracture.Services;

/// <summary>
/// Lists and toggles Windows startup programs. Disabling is done reversibly by moving
/// registry values into a Fracture backup key, or moving shortcut files into a
/// "FractureDisabled" subfolder — so nothing is destroyed and everything can be restored.
/// </summary>
public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string BackupKeyPath = @"Software\Fracture\StartupBackup";
    private const string DisabledFolderName = "FractureDisabled";

    public static List<StartupEntry> List()
    {
        var entries = new List<StartupEntry>();

        // Registry: HKCU / HKLM Run (enabled) + backups (disabled)
        ReadRegistry(entries, Registry.CurrentUser, StartupLocation.HkcuRun, "HkcuRun");
        ReadRegistry(entries, Registry.LocalMachine, StartupLocation.HklmRun, "HklmRun");

        // Startup folders
        ReadFolder(entries,
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            StartupLocation.UserStartupFolder);
        ReadFolder(entries,
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            StartupLocation.CommonStartupFolder);

        return entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static void SetEnabled(StartupEntry entry, bool enabled)
    {
        if (entry.IsEnabled == enabled) return;

        switch (entry.Location)
        {
            case StartupLocation.HkcuRun:
                ToggleRegistry(Registry.CurrentUser, "HkcuRun", entry, enabled);
                break;
            case StartupLocation.HklmRun:
                ToggleRegistry(Registry.LocalMachine, "HklmRun", entry, enabled);
                break;
            case StartupLocation.UserStartupFolder:
                ToggleFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), entry, enabled);
                break;
            case StartupLocation.CommonStartupFolder:
                ToggleFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), entry, enabled);
                break;
        }

        entry.IsEnabled = enabled;
    }

    private static void ReadRegistry(List<StartupEntry> entries, RegistryKey root, StartupLocation location, string backupSub)
    {
        try
        {
            using var run = root.OpenSubKey(RunKeyPath);
            if (run != null)
            {
                foreach (var name in run.GetValueNames())
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    entries.Add(new StartupEntry
                    {
                        Name = name,
                        Command = run.GetValue(name)?.ToString() ?? string.Empty,
                        Location = location,
                        IsEnabled = true,
                    });
                }
            }
        }
        catch { }

        try
        {
            using var backup = root.OpenSubKey($@"{BackupKeyPath}\{backupSub}");
            if (backup != null)
            {
                foreach (var name in backup.GetValueNames())
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    entries.Add(new StartupEntry
                    {
                        Name = name,
                        Command = backup.GetValue(name)?.ToString() ?? string.Empty,
                        Location = location,
                        IsEnabled = false,
                    });
                }
            }
        }
        catch { }
    }

    private static void ToggleRegistry(RegistryKey root, string backupSub, StartupEntry entry, bool enable)
    {
        string backupPath = $@"{BackupKeyPath}\{backupSub}";

        if (enable)
        {
            using var backup = root.OpenSubKey(backupPath, writable: true)
                ?? throw new InvalidOperationException("Backup entry not found.");
            object? value = backup.GetValue(entry.Name);
            using var run = root.CreateSubKey(RunKeyPath, writable: true);
            run.SetValue(entry.Name, value ?? entry.Command);
            backup.DeleteValue(entry.Name, throwOnMissingValue: false);
        }
        else
        {
            using var run = root.OpenSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Run key not found.");
            object? value = run.GetValue(entry.Name);
            using var backup = root.CreateSubKey(backupPath, writable: true);
            backup.SetValue(entry.Name, value ?? entry.Command);
            run.DeleteValue(entry.Name, throwOnMissingValue: false);
        }
    }

    private static void ReadFolder(List<StartupEntry> entries, string folder, StartupLocation location)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                if (Path.GetFileName(file).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    continue;

                entries.Add(new StartupEntry
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Command = file,
                    Location = location,
                    IsEnabled = true,
                });
            }

            string disabled = Path.Combine(folder, DisabledFolderName);
            if (Directory.Exists(disabled))
            {
                foreach (var file in Directory.EnumerateFiles(disabled))
                {
                    entries.Add(new StartupEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Command = file,
                        Location = location,
                        IsEnabled = false,
                    });
                }
            }
        }
        catch { }
    }

    private static void ToggleFolder(string folder, StartupEntry entry, bool enable)
    {
        string disabledDir = Path.Combine(folder, DisabledFolderName);
        Directory.CreateDirectory(disabledDir);

        string fileName = Path.GetFileName(entry.Command);
        if (enable)
        {
            string from = Path.Combine(disabledDir, fileName);
            string to = Path.Combine(folder, fileName);
            if (File.Exists(from)) File.Move(from, to, overwrite: true);
        }
        else
        {
            string from = entry.Command;
            string to = Path.Combine(disabledDir, fileName);
            if (File.Exists(from)) File.Move(from, to, overwrite: true);
        }
    }
}
