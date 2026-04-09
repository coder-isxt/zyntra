namespace Zyntra.Models;

public class RecentGame
{
    public long PlaceId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.Now;
}
