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

    // Activity tracking
    public double TotalPlaytimeSeconds { get; set; }
    public int SessionCount { get; set; }
    public DateTime? LastPlayedAt { get; set; }

    /// <summary>Human-friendly total playtime, e.g. "3h 12m" or "45m".</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string PlaytimeText
    {
        get
        {
            var span = TimeSpan.FromSeconds(TotalPlaytimeSeconds);
            if (span.TotalMinutes < 1) return "Not played yet";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m played";
            return $"{(int)span.TotalHours}h {span.Minutes}m played";
        }
    }
}
