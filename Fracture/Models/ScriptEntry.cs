namespace Fracture.Models;

public class ScriptEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ScriptType { get; set; } = "Lua";
    public string Content { get; set; } = string.Empty;
    public string? Hotkey { get; set; }
    public bool RunOnStartup { get; set; }
    public bool SchedulerEnabled { get; set; }
    public int SchedulerIntervalMinutes { get; set; } = 60;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextScheduledRun { get; set; }
}
