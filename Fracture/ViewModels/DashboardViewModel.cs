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

    private int _scriptCount;
    public int ScriptCount
    {
        get => _scriptCount;
        set => SetProperty(ref _scriptCount, value);
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
        AppCount = AppStorageService.Load().Count;
        AccountCount = AccountStorageService.Load().Count;
        ScriptCount = ScriptService.Load().Count;
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
