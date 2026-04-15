using System.Windows;
using System.Windows.Controls;
using Zyntra.Models;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class RobloxAccountsView : UserControl
{
    public RobloxAccountsView()
    {
        InitializeComponent();
    }

    private void OnListViewClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RobloxAccountsViewModel vm)
            vm.IsGridView = false;
    }

    private void OnGridViewClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is RobloxAccountsViewModel vm)
            vm.IsGridView = true;
    }

    private void OnContextLaunch(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is RobloxAccount acc && DataContext is RobloxAccountsViewModel vm)
            vm.LaunchRobloxCommand.Execute(acc);
    }

    private void OnContextSetTag(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is RobloxAccount acc && DataContext is RobloxAccountsViewModel vm)
            vm.SetTagCommand.Execute(acc);
    }

    private void OnContextRefresh(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is RobloxAccount acc && DataContext is RobloxAccountsViewModel vm)
            vm.RefreshAccountCommand.Execute(acc);
    }

    private void OnContextRemove(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is RobloxAccount acc && DataContext is RobloxAccountsViewModel vm)
            vm.RemoveAccountCommand.Execute(acc);
    }

    private void OnCopyUsername(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is RobloxAccount acc)
            System.Windows.Clipboard.SetText(acc.Username);
    }

    private void OnCopyUserId(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is RobloxAccount acc)
            System.Windows.Clipboard.SetText(acc.UserId.ToString());
    }

    private async void OnAddAccountClick(object sender, RoutedEventArgs e)
    {
        string cookie = CookieInput.Text.Trim();
        if (string.IsNullOrEmpty(cookie))
        {
            MessageBox.Show("Please paste a .ROBLOSECURITY cookie.", "Zyntra",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DataContext is RobloxAccountsViewModel vm)
        {
            await vm.AddAccountWithCookieAsync(cookie);
            CookieInput.Text = string.Empty;
        }
    }

    private async void OnBrowserLoginClick(object sender, RoutedEventArgs e)
    {
        var loginWindow = new BrowserLoginWindow
        {
            Owner = Window.GetWindow(this)
        };

        bool? result = loginWindow.ShowDialog();

        if (result == true && !string.IsNullOrEmpty(loginWindow.CapturedCookie))
        {
            if (DataContext is RobloxAccountsViewModel vm)
            {
                await vm.AddAccountWithCookieAsync(loginWindow.CapturedCookie);
            }
        }
    }
}
