namespace Zyntra.Models;

public class ServerEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long PlaceId { get; set; }
    public string PlaceName { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
