namespace Zyntra.Models;

public class RobloxAccount
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EncryptedCookie { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
