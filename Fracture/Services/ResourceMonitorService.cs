using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Fracture.Services;

public record ResourceSample(
    double CpuPercent,
    double RamPercent,
    double RamUsedGb,
    double RamTotalGb,
    double DiskPercent,
    double? GpuPercent,
    double? CpuTempC);

/// <summary>
/// Samples live system resource usage. CPU and RAM use Win32 APIs (always available);
/// disk and GPU use performance counters; temperature uses WMI (best-effort, often N/A).
/// </summary>
public sealed class ResourceMonitorService : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);

    private long _prevIdle, _prevKernel, _prevUser;
    private bool _hasPrev;

    private PerformanceCounter? _diskCounter;
    private PerformanceCounter[]? _gpuCounters;
    private bool _gpuInitialized;
    private DateTime _lastGpuInit = DateTime.MinValue;

    public ResourceSample Read()
    {
        double cpu = ReadCpu();
        double ramPct = ReadRam(out double usedGb, out double totalGb);
        return new ResourceSample(cpu, ramPct, usedGb, totalGb, ReadDisk(), ReadGpu(), ReadCpuTemp());
    }

    private double ReadCpu()
    {
        if (!GetSystemTimes(out long idle, out long kernel, out long user))
            return 0;

        if (!_hasPrev)
        {
            _prevIdle = idle; _prevKernel = kernel; _prevUser = user;
            _hasPrev = true;
            return 0;
        }

        long idleDiff = idle - _prevIdle;
        long kernelDiff = kernel - _prevKernel;
        long userDiff = user - _prevUser;
        _prevIdle = idle; _prevKernel = kernel; _prevUser = user;

        // kernelTime already includes idle time.
        long total = kernelDiff + userDiff;
        if (total <= 0) return 0;

        double busy = total - idleDiff;
        return Math.Clamp(busy * 100.0 / total, 0, 100);
    }

    private double ReadRam(out double usedGb, out double totalGb)
    {
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref mem))
        {
            totalGb = mem.ullTotalPhys / (1024d * 1024d * 1024d);
            double availGb = mem.ullAvailPhys / (1024d * 1024d * 1024d);
            usedGb = totalGb - availGb;
            return mem.dwMemoryLoad;
        }

        usedGb = 0; totalGb = 0;
        return 0;
    }

    private double ReadDisk()
    {
        try
        {
            _diskCounter ??= new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            return Math.Clamp(_diskCounter.NextValue(), 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    private double? ReadGpu()
    {
        try
        {
            // Re-enumerate GPU engine instances occasionally (they come and go).
            if (!_gpuInitialized || (DateTime.Now - _lastGpuInit).TotalSeconds > 15)
            {
                _lastGpuInit = DateTime.Now;
                var category = new PerformanceCounterCategory("GPU Engine");
                var names = category.GetInstanceNames()
                    .Where(n => n.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                _gpuCounters = names
                    .Select(n => new PerformanceCounter("GPU Engine", "Utilization Percentage", n))
                    .ToArray();
                _gpuInitialized = true;
            }

            if (_gpuCounters == null || _gpuCounters.Length == 0)
                return null;

            double sum = 0;
            foreach (var c in _gpuCounters)
            {
                try { sum += c.NextValue(); } catch { }
            }
            return Math.Clamp(sum, 0, 100);
        }
        catch
        {
            return null;
        }
    }

    private double? ReadCpuTemp()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (var obj in searcher.Get())
            {
                double tenthKelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                return (tenthKelvin / 10.0) - 273.15;
            }
        }
        catch
        {
            // Not supported on most consumer hardware without a kernel driver.
        }
        return null;
    }

    public void Dispose()
    {
        _diskCounter?.Dispose();
        if (_gpuCounters != null)
            foreach (var c in _gpuCounters) c.Dispose();
    }
}
