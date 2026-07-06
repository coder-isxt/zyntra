using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Fracture.Models;
using Fracture.Services;

namespace Fracture.ViewModels;

public class OptimizationActionVM : BaseViewModel
{
    private readonly Func<Task<string>> _run;
    private readonly string? _confirmMessage;

    public string Icon { get; }
    public string Title { get; }
    public string Description { get; }
    public bool RequiresAdmin { get; }
    public bool CreatesRestorePoint { get; }

    public bool ShowAdminBadge => RequiresAdmin;

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    private string _resultText = string.Empty;
    public string ResultText
    {
        get => _resultText;
        set
        {
            if (SetProperty(ref _resultText, value))
                OnPropertyChanged(nameof(HasResult));
        }
    }

    public bool HasResult => !string.IsNullOrEmpty(ResultText);

    public ICommand RunCommand { get; }

    public OptimizationActionVM(string icon, string title, string description,
        Func<Task<string>> run, string? confirmMessage = null,
        bool requiresAdmin = false, bool createsRestorePoint = false)
    {
        Icon = icon;
        Title = title;
        Description = description;
        _run = run;
        _confirmMessage = confirmMessage;
        RequiresAdmin = requiresAdmin;
        CreatesRestorePoint = createsRestorePoint;
        RunCommand = new RelayCommand(async _ => await RunAsync(), _ => !IsRunning);
    }

    public async Task RunAsync()
    {
        if (IsRunning) return;

        // 1. Elevation gate
        if (RequiresAdmin && !AdminService.IsElevated)
        {
            AdminService.PromptRelaunchAsAdmin(
                $"\"{Title}\" needs administrator rights to run.");
            return; // either relaunching, or the user declined
        }

        // 2. Confirmation for destructive actions
        if (_confirmMessage != null)
        {
            var confirm = MessageBox.Show(_confirmMessage, "Fracture",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        // 3. Optional system restore point
        if (CreatesRestorePoint)
        {
            var rp = MessageBox.Show(
                "Create a system restore point before continuing? (recommended)",
                "Fracture", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (rp == MessageBoxResult.Cancel) return;
            if (rp == MessageBoxResult.Yes)
            {
                IsRunning = true;
                ResultText = "Creating restore point...";
                string rpResult = await SystemRestoreService.CreateRestorePointAsync("Fracture optimization");
                ActivityLogService.Log(ActivityKind.Optimization, "System restore point", rpResult);
                IsRunning = false;
            }
        }

        await RunAsyncNoConfirm();
    }

    /// <summary>Runs the action without confirmation/elevation prompts (used by "Run all").</summary>
    public async Task RunAsyncNoConfirm()
    {
        if (IsRunning) return;

        IsRunning = true;
        ResultText = "Working...";
        try
        {
            ResultText = await _run();
            ActivityLogService.Log(ActivityKind.Optimization, Title, ResultText);
        }
        catch (Exception ex)
        {
            ResultText = $"Failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }
}

public class OptimizationViewModel : BaseViewModel, IDisposable
{
    private readonly ResourceMonitorService _monitor = new();
    private readonly DispatcherTimer _timer;
    private bool _sampling;

    public ObservableCollection<OptimizationActionVM> QuickActions { get; } = new();
    public ObservableCollection<OptimizationActionVM> AdvancedActions { get; } = new();

    // ---- Live meters ----
    private double _cpuPercent;
    public double CpuPercent { get => _cpuPercent; set => SetProperty(ref _cpuPercent, value); }
    private string _cpuText = "—";
    public string CpuText { get => _cpuText; set => SetProperty(ref _cpuText, value); }

    private double _ramPercent;
    public double RamPercent { get => _ramPercent; set => SetProperty(ref _ramPercent, value); }
    private string _ramText = "—";
    public string RamText { get => _ramText; set => SetProperty(ref _ramText, value); }

    private double _diskPercent;
    public double DiskPercent { get => _diskPercent; set => SetProperty(ref _diskPercent, value); }
    private string _diskText = "—";
    public string DiskText { get => _diskText; set => SetProperty(ref _diskText, value); }

    private double _gpuPercent;
    public double GpuPercent { get => _gpuPercent; set => SetProperty(ref _gpuPercent, value); }
    private string _gpuText = "—";
    public string GpuText { get => _gpuText; set => SetProperty(ref _gpuText, value); }

    private string _tempText = "—";
    public string TempText { get => _tempText; set => SetProperty(ref _tempText, value); }

    private string _statusText = "Run a tune-up to free space and speed things up.";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private bool _isElevated = AdminService.IsElevated;
    public bool IsElevated { get => _isElevated; set => SetProperty(ref _isElevated, value); }
    public bool ShowElevatePrompt => !IsElevated;

    public ICommand RunAllCommand { get; }
    public ICommand ElevateCommand { get; }

    public OptimizationViewModel()
    {
        // Quick (safe) optimizations
        QuickActions.Add(new OptimizationActionVM("🧹", "Clear temporary files",
            "Delete leftover files in the Windows temp folders to free disk space.",
            OptimizationService.ClearTempFilesAsync,
            "Delete all temporary files? Apps may recreate what they need."));

        QuickActions.Add(new OptimizationActionVM("🗑️", "Empty Recycle Bin",
            "Permanently remove everything in the Recycle Bin on all drives.",
            OptimizationService.EmptyRecycleBinAsync,
            "Permanently empty the Recycle Bin on all drives?"));

        QuickActions.Add(new OptimizationActionVM("🧠", "Free up memory",
            "Trim the working set of running processes to release standby RAM.",
            OptimizationService.FreeMemoryAsync));

        QuickActions.Add(new OptimizationActionVM("⚡", "High Performance power plan",
            "Switch Windows to the High Performance power plan for maximum speed.",
            OptimizationService.SetHighPerformancePowerPlanAsync));

        QuickActions.Add(new OptimizationActionVM("🌐", "Flush DNS cache",
            "Clear the DNS resolver cache to fix stale lookups and connection hiccups.",
            OptimizationService.FlushDnsAsync));

        // Advanced (destructive / admin) optimizations
        AdvancedActions.Add(new OptimizationActionVM("🧯", "Deep system cleanup",
            "Clear Windows Update cache, Prefetch, thumbnails and crash dumps.",
            OptimizationService.DeepCleanAsync,
            "Run a deep cleanup? This clears system caches (Windows regenerates them).",
            requiresAdmin: true, createsRestorePoint: true));

        AdvancedActions.Add(new OptimizationActionVM("🛑", "Close background apps",
            "Terminate common tray/background helpers (OneDrive, Teams, Spotify, etc.).",
            OptimizationService.CloseBackgroundAppsAsync,
            "Close known background apps now? Unsaved work in them may be lost."));

        AdvancedActions.Add(new OptimizationActionVM("🎛️", "Apply performance tweaks",
            "Set visual effects to best performance and disable Game DVR.",
            OptimizationService.ApplyPerformanceTweaksAsync,
            "Apply performance registry tweaks?",
            createsRestorePoint: true));

        AdvancedActions.Add(new OptimizationActionVM("🧠", "Purge standby memory",
            "Release the Windows standby (cached) memory list. Requires admin.",
            OptimizationService.PurgeStandbyMemoryAsync,
            requiresAdmin: true));

        AdvancedActions.Add(new OptimizationActionVM("📦", "Clean component store (WinSxS)",
            "Reclaim disk space from superseded Windows components. Requires admin.",
            OptimizationService.CleanComponentStoreAsync,
            "Run DISM component cleanup? This can take several minutes.",
            requiresAdmin: true, createsRestorePoint: true));

        AdvancedActions.Add(new OptimizationActionVM("🩺", "Repair system files (SFC + DISM)",
            "Scan and repair corrupted Windows system files. Requires admin, slow.",
            OptimizationService.RepairSystemFilesAsync,
            "Run SFC and DISM repair? This can take 10+ minutes.",
            requiresAdmin: true, createsRestorePoint: true));

        AdvancedActions.Add(new OptimizationActionVM("🔌", "Reset network stack",
            "Reset Winsock and TCP/IP, flush DNS, and renew your IP. Requires admin.",
            OptimizationService.ResetNetworkAsync,
            "Reset the network stack? You'll need to restart afterwards.",
            requiresAdmin: true, createsRestorePoint: true));

        AdvancedActions.Add(new OptimizationActionVM("🚫", "Stop telemetry services",
            "Stop and disable Windows diagnostic/telemetry background services. Requires admin.",
            OptimizationService.StopTelemetryServicesAsync,
            "Stop and disable telemetry services?",
            requiresAdmin: true, createsRestorePoint: true));

        RunAllCommand = new RelayCommand(async _ => await RunAllAsync());
        ElevateCommand = new RelayCommand(_ => AdminService.RelaunchAsAdmin());

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        if (_sampling) return;
        _sampling = true;
        try
        {
            var s = await Task.Run(() => _monitor.Read());
            CpuPercent = s.CpuPercent;
            CpuText = $"{s.CpuPercent:0}%";
            RamPercent = s.RamPercent;
            RamText = $"{s.RamPercent:0}%  ·  {s.RamUsedGb:0.0} / {s.RamTotalGb:0.0} GB";
            DiskPercent = s.DiskPercent;
            DiskText = $"{s.DiskPercent:0}%";
            GpuPercent = s.GpuPercent ?? 0;
            GpuText = s.GpuPercent.HasValue ? $"{s.GpuPercent:0}%" : "N/A";
            TempText = s.CpuTempC.HasValue ? $"{s.CpuTempC:0}°C" : "N/A";
        }
        catch { }
        finally { _sampling = false; }
    }

    private async Task RunAllAsync()
    {
        var confirm = MessageBox.Show(
            "Run all quick optimizations now? This clears temp files, empties the Recycle Bin, frees memory, sets the power plan, and flushes DNS.",
            "Fracture", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        StatusText = "Running quick optimizations...";
        foreach (var action in QuickActions)
            await action.RunAsyncNoConfirm();
        StatusText = "Quick optimizations finished.";
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _monitor.Dispose();
    }
}
