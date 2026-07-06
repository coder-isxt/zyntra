namespace Fracture.Models;

public enum StartupLocation
{
    HkcuRun,
    HklmRun,
    UserStartupFolder,
    CommonStartupFolder
}

public class StartupEntry
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public StartupLocation Location { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>Whether toggling this entry requires administrator rights.</summary>
    public bool RequiresAdmin =>
        Location is StartupLocation.HklmRun or StartupLocation.CommonStartupFolder;

    public string LocationText => Location switch
    {
        StartupLocation.HkcuRun => "Registry (current user)",
        StartupLocation.HklmRun => "Registry (all users)",
        StartupLocation.UserStartupFolder => "Startup folder (current user)",
        StartupLocation.CommonStartupFolder => "Startup folder (all users)",
        _ => "Unknown"
    };
}
