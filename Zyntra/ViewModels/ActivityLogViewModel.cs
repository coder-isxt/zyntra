using System.Collections.ObjectModel;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class ActivityLogViewModel : BaseViewModel
{
    public ObservableCollection<ActivityLogEntry> Entries => ActivityLogService.Entries;

    private string _filterText = string.Empty;
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                OnPropertyChanged(nameof(FilteredEntries));
        }
    }

    public IEnumerable<ActivityLogEntry> FilteredEntries
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_filterText))
                return Entries;
            return Entries.Where(e =>
                e.Action.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                e.Details.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                (e.AccountName?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.PlaceName?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }

    public ICommand ClearLogCommand { get; }
    public ICommand RefreshCommand { get; }

    public ActivityLogViewModel()
    {
        ClearLogCommand = new RelayCommand(_ =>
        {
            ActivityLogService.Clear();
            OnPropertyChanged(nameof(FilteredEntries));
        });
        RefreshCommand = new RelayCommand(_ =>
        {
            ActivityLogService.Load();
            OnPropertyChanged(nameof(FilteredEntries));
        });

        ActivityLogService.Load();
    }
}
