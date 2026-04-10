namespace Zyntra.Models;

public class ScriptEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ScriptType { get; set; } = "Lua";
    public string Content { get; set; } = string.Empty;
    public string? Hotkey { get; set; }
    public bool RunOnStartup { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }
}
