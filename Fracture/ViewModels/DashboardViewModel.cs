using System.Windows.Input;
using Fracture.Services;

namespace Fracture.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private int _appCount;
    public int AppCount
    {
        get => _appCount;
        set => SetProperty(ref _appCount, value);
    }

    private int _accountCount;
    public int AccountCount
    {
        get => _accountCount;
        set => SetProperty(ref _accountCount, value);
    }

    private string _playtimeText = "0h 0m";
    public string PlaytimeText
    {
        get => _playtimeText;
        set => SetProperty(ref _playtimeText, value);
    }

    private string _greeting = string.Empty;
    public string Greeting
    {
        get => _greeting;
        set => SetProperty(ref _greeting, value);
    }

    private string _version = string.Empty;
    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    public ICommand NavigateCommand { get; set; } = null!;

    public DashboardViewModel()
    {
        Version = $"v{UpdateService.CurrentVersion}";
        UpdateGreeting();
    }

    public void Refresh()
    {
        var accounts = AccountStorageService.Load();
        AppCount = AppStorageService.Load().Count;
        AccountCount = accounts.Count;

        double totalSeconds = accounts.Sum(a => a.TotalPlaytimeSeconds);
        var span = TimeSpan.FromSeconds(totalSeconds);
        PlaytimeText = $"{(int)span.TotalHours}h {span.Minutes}m";

        UpdateGreeting();
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        Greeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            < 21 => "Good evening",
            _ => "Good night"
        };
    }
}
