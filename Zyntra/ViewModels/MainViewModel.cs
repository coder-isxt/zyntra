using System.Windows.Input;

namespace Zyntra.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentPage;
    public BaseViewModel CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private string _currentPageName = "Apps";
    public string CurrentPageName
    {
        get => _currentPageName;
        set => SetProperty(ref _currentPageName, value);
    }

    public AppsViewModel AppsVM { get; }
    public RobloxAccountsViewModel RobloxVM { get; }
    public SteamViewModel SteamVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public ICommand NavigateCommand { get; }

    public MainViewModel()
    {
        AppsVM = new AppsViewModel();
        RobloxVM = new RobloxAccountsViewModel();
        SteamVM = new SteamViewModel();
        SettingsVM = new SettingsViewModel();
        _currentPage = AppsVM;

        NavigateCommand = new RelayCommand(Navigate);
    }

    private void Navigate(object? param)
    {
        string page = param as string ?? "Apps";
        switch (page)
        {
            case "Apps":
                CurrentPage = AppsVM;
                CurrentPageName = "Apps";
                break;
            case "Roblox":
                CurrentPage = RobloxVM;
                CurrentPageName = "Roblox Accounts";
                break;
            case "Steam":
                CurrentPage = SteamVM;
                CurrentPageName = "Steam";
                break;
            case "Settings":
                CurrentPage = SettingsVM;
                CurrentPageName = "Settings";
                break;
        }
    }
}
