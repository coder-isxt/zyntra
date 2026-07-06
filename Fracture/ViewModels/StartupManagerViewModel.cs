using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Fracture.Models;
using Fracture.Services;

namespace Fracture.ViewModels;

public class StartupEntryVM : BaseViewModel
{
    private readonly StartupManagerViewModel _owner;
    public StartupEntry Model { get; }

    public string Name => Model.Name;
    public string Command => Model.Command;
    public string LocationText => Model.LocationText;
    public bool RequiresAdmin => Model.RequiresAdmin;
    public bool ShowAdminBadge => Model.RequiresAdmin;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            if (!_owner.TryToggle(this, value))
            {
                // Revert the toggle in the UI if it couldn't be applied.
                _isEnabled = !value;
                OnPropertyChanged();
                return;
            }
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    public StartupEntryVM(StartupManagerViewModel owner, StartupEntry model)
    {
        _owner = owner;
        Model = model;
        _isEnabled = model.IsEnabled;
    }
}

public class StartupManagerViewModel : BaseViewModel
{
    public ObservableCollection<StartupEntryVM> Entries { get; } = new();

    private string _statusText = "Disable programs you don't need at startup to boot faster.";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand RefreshCommand { get; }

    public StartupManagerViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        Load();
    }

    public void Load()
    {
        Entries.Clear();
        try
        {
            foreach (var entry in StartupService.List())
                Entries.Add(new StartupEntryVM(this, entry));

            int enabled = Entries.Count(e => e.IsEnabled);
            StatusText = $"{Entries.Count} startup item(s) — {enabled} enabled";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to read startup items: {ex.Message}";
        }
    }

    /// <summary>Attempts to enable/disable an entry. Returns false if it was not applied.</summary>
    public bool TryToggle(StartupEntryVM entry, bool enable)
    {
        if (entry.RequiresAdmin && !AdminService.IsElevated)
        {
            AdminService.PromptRelaunchAsAdmin(
                $"Changing \"{entry.Name}\" requires administrator rights.");
            return false;
        }

        try
        {
            StartupService.SetEnabled(entry.Model, enable);
            ActivityLogService.Log(ActivityKind.Optimization,
                $"{(enable ? "Enabled" : "Disabled")} startup: {entry.Name}");
            StatusText = $"{(enable ? "Enabled" : "Disabled")} {entry.Name}";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not change startup item: {ex.Message}",
                "Fracture", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"Failed to change {entry.Name}";
            return false;
        }
    }
}
