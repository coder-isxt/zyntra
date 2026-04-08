using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Zyntra.Services;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AboutVersionText.Text = $"Zyntra v{UpdateService.CurrentVersion}";
        BuildAccentPicker();
    }

    private void BuildAccentPicker()
    {
        AccentPanel.Children.Clear();
        var vm = DataContext as SettingsViewModel;
        if (vm == null) return;

        foreach (var opt in vm.AccentOptions)
        {
            var color = (Color)ColorConverter.ConvertFromString(opt.Hex);
            var grid = new Grid { Width = 36, Height = 36, Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 8, 8) };
            grid.ToolTip = opt.Name;

            var circle = new Ellipse { Width = 36, Height = 36, Fill = new SolidColorBrush(color) };
            grid.Children.Add(circle);

            if (opt.IsSelected)
            {
                var ring = new Ellipse
                {
                    Width = 36, Height = 36, StrokeThickness = 2.5,
                    Stroke = (SolidColorBrush)FindResource("TextBrush"),
                };
                grid.Children.Add(ring);

                var dot = new Ellipse
                {
                    Width = 14, Height = 14,
                    Fill = new SolidColorBrush(Colors.White), Opacity = 0.9,
                };
                grid.Children.Add(dot);
            }

            string hex = opt.Hex;
            grid.MouseLeftButtonDown += (_, _) =>
            {
                vm.SetAccentCommand.Execute(hex);
                BuildAccentPicker();
            };

            AccentPanel.Children.Add(grid);
        }
    }
}
