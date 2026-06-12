namespace Fracture.Models;

public class RobloxAccount
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EncryptedCookie { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string Tag { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool? CookieValid { get; set; }
    public DateTime? LastHealthCheck { get; set; }
}
