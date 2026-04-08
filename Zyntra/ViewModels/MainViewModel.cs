using System.Collections.ObjectModel;
using System.Windows.Input;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentPage;
    public BaseViewModel CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private string _currentPageName = "Dashboard";
    public string CurrentPageName
    {
        get => _currentPageName;
        set => SetProperty(ref _currentPageName, value);
    }

    private int _unreadNotifications;
    public int UnreadNotifications
    {
        get => _unreadNotifications;
        set => SetProperty(ref _unreadNotifications, value);
    }

    private bool _notificationPanelOpen;
    public bool NotificationPanelOpen
    {
        get => _notificationPanelOpen;
        set => SetProperty(ref _notificationPanelOpen, value);
    }

    public ObservableCollection<NotificationItem> Notifications => NotificationService.Notifications;

    public DashboardViewModel DashboardVM { get; }
    public AppsViewModel AppsVM { get; }
    public RobloxAccountsViewModel RobloxVM { get; }
    public PluginsViewModel PluginsVM { get; }
    public ScriptsViewModel ScriptsVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public ICommand NavigateCommand { get; }
    public ICommand ToggleNotificationsCommand { get; }
    public ICommand MarkAllReadCommand { get; }
    public ICommand ClearNotificationsCommand { get; }

    public MainViewModel()
    {
        DashboardVM = new DashboardViewModel();
        AppsVM = new AppsViewModel();
        RobloxVM = new RobloxAccountsViewModel();
        PluginsVM = new PluginsViewModel();
        ScriptsVM = new ScriptsViewModel();
        SettingsVM = new SettingsViewModel();
        _currentPage = DashboardVM;

        NavigateCommand = new RelayCommand(Navigate);
        DashboardVM.NavigateCommand = NavigateCommand;

        ToggleNotificationsCommand = new RelayCommand(_ =>
        {
            NotificationPanelOpen = !NotificationPanelOpen;
            if (NotificationPanelOpen)
            {
                NotificationService.MarkAllRead();
                UnreadNotifications = 0;
            }
        });
        MarkAllReadCommand = new RelayCommand(_ =>
        {
            NotificationService.MarkAllRead();
            UnreadNotifications = 0;
        });
        ClearNotificationsCommand = new RelayCommand(_ =>
        {
            NotificationService.Clear();
            UnreadNotifications = 0;
        });

        NotificationService.OnChanged += () =>
        {
            UnreadNotifications = NotificationService.UnreadCount;
        };
    }

    private void Navigate(object? param)
    {
        string page = param as string ?? "Dashboard";
        NotificationPanelOpen = false;

        switch (page)
        {
            case "Dashboard":
                DashboardVM.Refresh();
                CurrentPage = DashboardVM;
                CurrentPageName = "Dashboard";
                break;
            case "Apps":
                CurrentPage = AppsVM;
                CurrentPageName = "Apps";
                break;
            case "Roblox":
                CurrentPage = RobloxVM;
                CurrentPageName = "Roblox Accounts";
                break;
            case "Plugins":
                CurrentPage = PluginsVM;
                CurrentPageName = "Plugins";
                break;
            case "Scripts":
                CurrentPage = ScriptsVM;
                CurrentPageName = "Scripts";
                break;
            case "Settings":
                CurrentPage = SettingsVM;
                CurrentPageName = "Settings";
                break;
        }
    }
}
