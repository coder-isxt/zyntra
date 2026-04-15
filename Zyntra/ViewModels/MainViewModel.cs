using System.Collections.ObjectModel;
using System.Windows.Input;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel _currentPage = null!;
    public BaseViewModel CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private string _currentPageName = "Roblox Accounts";
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

    // Sidebar badge counts
    private int _accountCount;
    public int AccountCount { get => _accountCount; set => SetProperty(ref _accountCount, value); }

    private int _appCount;
    public int AppCount { get => _appCount; set => SetProperty(ref _appCount, value); }

    private int _scriptCount;
    public int ScriptCount { get => _scriptCount; set => SetProperty(ref _scriptCount, value); }

    private bool _showSidebarBadges;
    public bool ShowSidebarBadges { get => _showSidebarBadges; set => SetProperty(ref _showSidebarBadges, value); }

    public ObservableCollection<NotificationItem> Notifications => NotificationService.Notifications;
    public ObservableCollection<ToastItem> Toasts => ToastService.ActiveToasts;
    public ObservableCollection<ScriptTab> ScriptTabs => ScriptUIService.Tabs;

    public AppsViewModel AppsVM { get; }
    public RobloxAccountsViewModel RobloxVM { get; }
    public ScriptsViewModel ScriptsVM { get; }
    public DocsViewModel DocsVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public ICommand NavigateCommand { get; }
    public ICommand ToggleNotificationsCommand { get; }
    public ICommand MarkAllReadCommand { get; }
    public ICommand ClearNotificationsCommand { get; }

    public MainViewModel()
    {
        AppsVM = new AppsViewModel();
        RobloxVM = new RobloxAccountsViewModel();
        ScriptsVM = new ScriptsViewModel();
        DocsVM = new DocsViewModel();
        SettingsVM = new SettingsViewModel();

        // Sidebar badges
        var settings = SettingsService.Load();
        _showSidebarBadges = settings.ShowSidebarBadges;
        RefreshBadgeCounts();

        // Load favorite games
        FavoriteGamesService.Load();

        // Start script scheduler
        ScriptSchedulerService.Start();

        // Navigate to the saved default page
        NavigateCommand = new RelayCommand(Navigate);
        Navigate(SettingsVM.DefaultPage);

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

        // Listen for settings changes to update sidebar badges toggle
        SettingsVM.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.ShowSidebarBadges))
                ShowSidebarBadges = SettingsVM.ShowSidebarBadges;
        };
    }

    public void NavigateToScriptTab(string tabId)
    {
        var tab = ScriptUIService.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            CurrentPage = new ScriptTabViewModel(tab);
            CurrentPageName = tab.Name;
        }
    }

    public void RefreshBadgeCounts()
    {
        AccountCount = AccountStorageService.Load().Count;
        AppCount = AppStorageService.Load().Count;
        ScriptCount = ScriptService.Load().Count;
    }

    private void Navigate(object? param)
    {
        string page = param as string ?? "Roblox";
        NotificationPanelOpen = false;

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
            case "Scripts":
                CurrentPage = ScriptsVM;
                CurrentPageName = "Scripts";
                break;
            case "Docs":
                CurrentPage = DocsVM;
                CurrentPageName = "API Docs";
                break;
            case "Settings":
                CurrentPage = SettingsVM;
                CurrentPageName = "Settings";
                break;
        }
    }
}
