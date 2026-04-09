namespace Zyntra.Models;

public class ActivityLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? AccountName { get; set; }
    public long? PlaceId { get; set; }
    public string? PlaceName { get; set; }
}
