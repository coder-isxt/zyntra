using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class AppsViewModel : BaseViewModel
{
    public ObservableCollection<AppEntry> Apps { get; } = new();

    private AppEntry? _selectedApp;
    public AppEntry? SelectedApp
    {
        get => _selectedApp;
        set => SetProperty(ref _selectedApp, value);
    }

    public ICommand LaunchAppCommand { get; }
    public ICommand RemoveAppCommand { get; }
    public ICommand AddAppCommand { get; }
    public ICommand EditAppCommand { get; }

    public AppsViewModel()
    {
        LaunchAppCommand = new RelayCommand(LaunchApp);
        RemoveAppCommand = new RelayCommand(RemoveApp);
        AddAppCommand = new RelayCommand(_ => AddApp());
        EditAppCommand = new RelayCommand(EditApp);

        LoadApps();
    }

    private void LoadApps()
    {
        Apps.Clear();

        var customApps = AppStorageService.Load();
        foreach (var app in customApps)
            Apps.Add(app);
    }

    private void LaunchApp(object? param)
    {
        var app = param as AppEntry ?? SelectedApp;
        if (app == null) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = app.ExePath,
                UseShellExecute = !app.IsGameModule,
            };

            if (app.IsGameModule)
            {
                if (!string.IsNullOrEmpty(app.LaunchArgs))
                    psi.Arguments = app.LaunchArgs;

                if (!string.IsNullOrEmpty(app.WorkingDirectory))
                    psi.WorkingDirectory = app.WorkingDirectory;
                else
                    psi.WorkingDirectory = System.IO.Path.GetDirectoryName(app.ExePath) ?? string.Empty;

                if (!string.IsNullOrEmpty(app.EnvironmentVars))
                {
                    foreach (var line in app.EnvironmentVars.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                            psi.EnvironmentVariables[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch {app.Name}: {ex.Message}", "Zyntra", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveApp(object? param)
    {
        var app = param as AppEntry ?? SelectedApp;
        if (app == null || app.IsBuiltIn) return;

        Apps.Remove(app);
        SaveCustomApps();
    }

    public void AddApp()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Application",
            Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*",
        };

        if (dialog.ShowDialog() != true) return;

        var settings = new Views.AppSettingsWindow
        {
            Owner = Application.Current.MainWindow,
        };
        settings.SetDefaults(dialog.FileName, System.IO.Path.GetFileNameWithoutExtension(dialog.FileName));

        if (settings.ShowDialog() == true)
        {
            var entry = settings.ResultEntry;
            entry.IsBuiltIn = false;
            Apps.Add(entry);
            SaveCustomApps();
        }
    }

    private void EditApp(object? param)
    {
        var app = param as AppEntry ?? SelectedApp;
        if (app == null) return;

        var settings = new Views.AppSettingsWindow
        {
            Owner = Application.Current.MainWindow,
        };
        settings.LoadFromEntry(app);

        if (settings.ShowDialog() == true)
        {
            int idx = Apps.IndexOf(app);
            if (idx >= 0)
            {
                Apps.RemoveAt(idx);
                Apps.Insert(idx, settings.ResultEntry);
            }
            SaveCustomApps();
        }
    }

    private void SaveCustomApps()
    {
        var custom = Apps.Where(a => !a.IsBuiltIn).ToList();
        AppStorageService.Save(custom);
    }
}
