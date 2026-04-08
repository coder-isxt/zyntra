namespace Zyntra.Models;

public class AppEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public string? Description { get; set; }
    public bool IsBuiltIn { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
