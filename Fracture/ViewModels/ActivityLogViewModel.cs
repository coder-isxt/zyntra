using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Fracture.Models;
using Fracture.Services;

namespace Fracture.ViewModels;

public class ActivityLogViewModel : BaseViewModel
{
    public ObservableCollection<ActivityLogEntry> Entries => ActivityLogService.Entries;

    public ICommand ClearCommand { get; }

    public ActivityLogViewModel()
    {
        ActivityLogService.Load();
        ClearCommand = new RelayCommand(_ => Clear());
    }

    private void Clear()
    {
        var result = MessageBox.Show(
            "Clear the entire activity log?",
            "Fracture", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            ActivityLogService.Clear();
    }
}
