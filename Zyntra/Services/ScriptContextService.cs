namespace Zyntra.Services;

public class ScriptResponseLaunch
{
    public string AccountName { get; set; } = string.Empty;
    public long PlaceId { get; set; }
}

public class ScriptResponseNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, Success, Warning, Error
}
