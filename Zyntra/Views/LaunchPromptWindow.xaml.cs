using System.Windows;
using System.Windows.Input;

namespace Zyntra.Views;

public partial class LaunchPromptWindow : Window
{
    public long? PlaceId { get; private set; }
    public bool JustLaunch { get; private set; }

    public string AccountName
    {
        set => AccountNameRun.Text = value;
    }

    public LaunchPromptWindow()
    {
        InitializeComponent();
    }

    private void OnJoinGameClick(object sender, RoutedEventArgs e)
    {
        string text = PlaceIdInput.Text.Trim();
        if (string.IsNullOrEmpty(text) || !long.TryParse(text, out long placeId) || placeId <= 0)
        {
            MessageBox.Show("Please enter a valid Place ID.", "Zyntra",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PlaceId = placeId;
        JustLaunch = false;
        DialogResult = true;
        Close();
    }

    private void OnJustLaunchClick(object sender, RoutedEventArgs e)
    {
        PlaceId = null;
        JustLaunch = true;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
