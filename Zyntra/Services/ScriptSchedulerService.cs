using System.Timers;
using Zyntra.Models;

namespace Zyntra.Services;

public static class ScriptSchedulerService
{
    private static System.Timers.Timer? _timer;

    public static void Start()
    {
        _timer = new System.Timers.Timer(30_000); // Check every 30 seconds
        _timer.Elapsed += OnTick;
        _timer.AutoReset = true;
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private static async void OnTick(object? sender, ElapsedEventArgs e)
    {
        var scripts = ScriptService.Load();
        bool needsSave = false;

        foreach (var script in scripts.Where(s => s.SchedulerEnabled))
        {
            DateTime nextRun = script.NextScheduledRun ?? script.LastRunAt?.AddMinutes(script.SchedulerIntervalMinutes) ?? DateTime.MinValue;

            if (DateTime.UtcNow >= nextRun)
            {
                try
                {
                    await ScriptService.RunAsync(script);
                    script.LastRunAt = DateTime.UtcNow;
                    script.NextScheduledRun = DateTime.UtcNow.AddMinutes(script.SchedulerIntervalMinutes);
                    needsSave = true;

                    ToastService.Show("Scheduler", $"Ran \"{script.Name}\"", NotificationType.Info);
                    NotificationService.Push("Scheduler", $"Ran \"{script.Name}\" automatically", NotificationType.Info);
                }
                catch (Exception ex)
                {
                    ToastService.Show("Scheduler Error", $"{script.Name}: {ex.Message}", NotificationType.Error);
                }
            }
        }

        if (needsSave)
            ScriptService.Save(scripts);
    }
}
