using System.Windows;
using System.Windows.Controls;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class RobloxAccountsView : UserControl
{
    public RobloxAccountsView()
    {
        InitializeComponent();
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
