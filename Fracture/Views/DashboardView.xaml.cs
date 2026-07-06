using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Fracture.ViewModels;

namespace Fracture.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.Refresh();
    }

    private MainViewModel? GetMain()
        => (Application.Current.MainWindow?.DataContext as MainViewModel);

    private void OnNavApps(object sender, MouseButtonEventArgs e)
        => GetMain()?.NavigateCommand.Execute("Apps");

    private void OnNavRoblox(object sender, MouseButtonEventArgs e)
        => GetMain()?.NavigateCommand.Execute("Roblox");

    private void OnNavActivity(object sender, MouseButtonEventArgs e)
        => GetMain()?.NavigateCommand.Execute("Activity");

    private void OnManageAccounts(object sender, RoutedEventArgs e)
        => GetMain()?.NavigateCommand.Execute("Roblox");

    private void OnQuickAddApp(object sender, RoutedEventArgs e)
    {
        var main = GetMain();
        main?.NavigateCommand.Execute("Apps");
        main?.AppsVM.AddApp();
    }

    private void OnQuickFastFlags(object sender, RoutedEventArgs e)
        => GetMain()?.NavigateCommand.Execute("FastFlags");
}
