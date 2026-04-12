namespace Zyntra.Models;

public class FavoriteGame
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long PlaceId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
