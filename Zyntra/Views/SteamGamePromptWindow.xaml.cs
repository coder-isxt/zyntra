using System.Windows;

namespace Zyntra.Views;

public partial class SteamGamePromptWindow : Window
{
    public long AppId { get; private set; }

    public SteamGamePromptWindow()
    {
        InitializeComponent();
        AppIdInput.Focus();
    }

    private void OnLaunchClick(object sender, RoutedEventArgs e)
    {
        if (long.TryParse(AppIdInput.Text.Trim(), out long id) && id > 0)
        {
            AppId = id;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please enter a valid Steam App ID.", "Zyntra",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
