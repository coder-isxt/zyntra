using System.Diagnostics;
using Fracture.Models;

namespace Fracture.Services;

/// <summary>
/// Tracks a launched Roblox process for an account and accumulates playtime
/// once the process exits. Also records launch/session-end activity log entries.
/// </summary>
public static class ActivityTrackerService
{
    /// <summary>
    /// Begins tracking a launched Roblox process. When the process exits, the
    /// elapsed time is added to the account's total playtime and <paramref name="onUpdated"/>
    /// is invoked on the UI thread so the caller can persist and refresh.
    /// </summary>
    public static void Track(RobloxAccount account, Process? process, Action? onUpdated)
    {
        if (process == null)
            return;

        var started = DateTime.Now;

        _ = Task.Run(async () =>
        {
            try
            {
                await process.WaitForExitAsync();
            }
            catch
            {
                // Process handle may be invalid; fall back to a minimal session.
            }

            var elapsed = DateTime.Now - started;
            if (elapsed.TotalSeconds < 0 || elapsed.TotalHours > 24)
                elapsed = TimeSpan.Zero;

            var dispatcher = System.Windows.Application.Current?.Dispatcher;

            void Apply()
            {
                account.TotalPlaytimeSeconds += elapsed.TotalSeconds;
                account.LastPlayedAt = DateTime.Now;

                ActivityLogService.Log(
                    Models.ActivityKind.SessionEnd,
                    $"{account.Username} played for {FormatDuration(elapsed)}",
                    $"Total: {account.PlaytimeText}");

                onUpdated?.Invoke();
            }

            if (dispatcher != null && !dispatcher.CheckAccess())
                dispatcher.Invoke(Apply);
            else
                Apply();
        });
    }

    public static string FormatDuration(TimeSpan span)
    {
        if (span.TotalMinutes < 1) return $"{(int)span.TotalSeconds}s";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m";
        return $"{(int)span.TotalHours}h {span.Minutes}m";
    }
}
