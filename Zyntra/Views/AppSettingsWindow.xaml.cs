using System.Windows;
using System.Windows.Input;
using Zyntra.Models;

namespace Zyntra.Views;

public partial class AppSettingsWindow : Window
{
    public AppEntry ResultEntry { get; private set; } = new();
    public bool IsEditMode { get; set; }

    public AppSettingsWindow()
    {
        InitializeComponent();
    }

    public void LoadFromEntry(AppEntry entry)
    {
        IsEditMode = true;
        TitleText.Text = $"Edit — {entry.Name}";
        NameInput.Text = entry.Name;
        DescInput.Text = entry.Description ?? string.Empty;
        ExePathInput.Text = entry.ExePath;
        IconPathInput.Text = entry.IconPath ?? string.Empty;
        GameModuleToggle.IsChecked = entry.IsGameModule;
        LaunchArgsInput.Text = entry.LaunchArgs ?? string.Empty;
        EnvVarsInput.Text = entry.EnvironmentVars ?? string.Empty;
        WorkDirInput.Text = entry.WorkingDirectory ?? string.Empty;
        GameModulePanel.Visibility = entry.IsGameModule ? Visibility.Visible : Visibility.Collapsed;

        ResultEntry = entry;
    }

    public void SetDefaults(string exePath, string name)
    {
        TitleText.Text = "Add Application";
        ExePathInput.Text = exePath;
        NameInput.Text = name;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        string name = NameInput.Text.Trim();
        string exePath = ExePathInput.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Please enter a name.", "Zyntra", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(exePath))
        {
            MessageBox.Show("Please select an executable.", "Zyntra", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultEntry.Name = name;
        ResultEntry.Description = string.IsNullOrWhiteSpace(DescInput.Text) ? null : DescInput.Text.Trim();
        ResultEntry.ExePath = exePath;
        ResultEntry.IconPath = string.IsNullOrWhiteSpace(IconPathInput.Text) ? null : IconPathInput.Text.Trim();
        ResultEntry.IsGameModule = GameModuleToggle.IsChecked == true;
        ResultEntry.LaunchArgs = string.IsNullOrWhiteSpace(LaunchArgsInput.Text) ? null : LaunchArgsInput.Text.Trim();
        ResultEntry.EnvironmentVars = string.IsNullOrWhiteSpace(EnvVarsInput.Text) ? null : EnvVarsInput.Text.Trim();
        ResultEntry.WorkingDirectory = string.IsNullOrWhiteSpace(WorkDirInput.Text) ? null : WorkDirInput.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void OnGameModuleToggled(object sender, RoutedEventArgs e)
    {
        GameModulePanel.Visibility = GameModuleToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnBrowseExeClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Executable",
            Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            ExePathInput.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(NameInput.Text))
                NameInput.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private void OnBrowseIconClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Icon",
            Filter = "Images (*.ico;*.png;*.jpg;*.bmp)|*.ico;*.png;*.jpg;*.bmp|Executables (*.exe)|*.exe|All Files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
            IconPathInput.Text = dialog.FileName;
    }

    private void OnClearIconClick(object sender, RoutedEventArgs e)
    {
        IconPathInput.Text = string.Empty;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        DragMove();
    }
}
