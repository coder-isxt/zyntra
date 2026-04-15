using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Zyntra.Models;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class AppsView : UserControl
{
    private System.Windows.Point _dragStartPoint;

    public AppsView()
    {
        InitializeComponent();
    }

    private void OnListViewClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppsViewModel vm)
            vm.IsGridView = false;
    }

    private void OnGridViewClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppsViewModel vm)
            vm.IsGridView = true;
    }

    private void OnGridCardLaunchApp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AppEntry app && DataContext is AppsViewModel vm)
            vm.LaunchAppCommand.Execute(app);
    }

    private void AppList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void AppList_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var diff = e.GetPosition(null) - _dragStartPoint;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        // Find the ListBoxItem under the mouse
        var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        if (item == null) return;

        var appEntry = item.DataContext as AppEntry;
        if (appEntry == null) return;

        DragDrop.DoDragDrop(item, appEntry, System.Windows.DragDropEffects.Move);
    }

    private void AppList_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(AppEntry)))
        {
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }
        e.Effects = System.Windows.DragDropEffects.Move;
        e.Handled = true;
    }

    private void AppList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(AppEntry))) return;

        var droppedApp = (AppEntry)e.Data.GetData(typeof(AppEntry));
        var target = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
        if (target == null) return;

        var targetApp = target.DataContext as AppEntry;
        if (targetApp == null || droppedApp == targetApp) return;

        var vm = DataContext as AppsViewModel;
        if (vm == null) return;

        vm.ReorderApp(droppedApp, targetApp);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T result) return result;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
