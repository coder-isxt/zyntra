using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class PluginsViewModel : BaseViewModel
{
    public ObservableCollection<PluginEntry> Plugins { get; } = new();

    private PluginEntry? _selectedPlugin;
    public PluginEntry? SelectedPlugin
    {
        get => _selectedPlugin;
        set => SetProperty(ref _selectedPlugin, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand InstallPluginCommand { get; }
    public ICommand RemovePluginCommand { get; }
    public ICommand TogglePluginCommand { get; }

    public PluginsViewModel()
    {
        InstallPluginCommand = new RelayCommand(_ => InstallPlugin());
        RemovePluginCommand = new RelayCommand(RemovePlugin);
        TogglePluginCommand = new RelayCommand(TogglePlugin);

        LoadPlugins();
    }

    private void LoadPlugins()
    {
        Plugins.Clear();
        foreach (var p in PluginService.LoadIndex())
            Plugins.Add(p);

        StatusText = $"{Plugins.Count} plugin(s) installed";
    }

    private void InstallPlugin()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Plugin DLL",
            Filter = "DLL Files (*.dll)|*.dll",
        };

        if (dialog.ShowDialog() != true) return;

        var entry = PluginService.InstallPlugin(dialog.FileName);
        if (entry != null)
        {
            Plugins.Add(entry);
            SavePlugins();
            StatusText = $"Installed {entry.Name}";
            NotificationService.Push("Plugin Installed", $"{entry.Name} has been installed.", NotificationType.Success);
        }
        else
        {
            MessageBox.Show("Failed to install plugin.", "Zyntra", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemovePlugin(object? param)
    {
        var plugin = param as PluginEntry ?? SelectedPlugin;
        if (plugin == null) return;

        Plugins.Remove(plugin);
        SavePlugins();
        StatusText = $"Removed {plugin.Name}";
        NotificationService.Push("Plugin Removed", $"{plugin.Name} has been removed.", NotificationType.Info);
    }

    private void TogglePlugin(object? param)
    {
        var plugin = param as PluginEntry ?? SelectedPlugin;
        if (plugin == null) return;

        plugin.IsEnabled = !plugin.IsEnabled;
        SavePlugins();

        // Force UI refresh
        int idx = Plugins.IndexOf(plugin);
        if (idx >= 0)
        {
            Plugins.RemoveAt(idx);
            Plugins.Insert(idx, plugin);
        }

        StatusText = $"{plugin.Name} {(plugin.IsEnabled ? "enabled" : "disabled")}";
    }

    private void SavePlugins()
    {
        PluginService.SaveIndex(Plugins.ToList());
    }
}
