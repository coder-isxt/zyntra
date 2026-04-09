using System.Windows;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

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
        LoadRecentGames();
    }

    private void LoadRecentGames()
    {
        var recent = RecentlyPlayedService.Games.Take(5).ToList();
        if (recent.Count > 0)
        {
            RecentList.ItemsSource = recent;
            RecentPanel.Visibility = Visibility.Visible;
        }
    }

    private void OnRecentGameClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is RecentGame game)
        {
            PlaceId = game.PlaceId;
            JustLaunch = false;
            DialogResult = true;
            Close();
        }
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
