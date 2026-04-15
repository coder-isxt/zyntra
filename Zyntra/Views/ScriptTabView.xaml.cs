using System.Windows;
using System.Windows.Controls;
using Zyntra.Services;
using Brush = System.Windows.Media.Brush;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using ProgressBar = System.Windows.Controls.ProgressBar;

namespace Zyntra.Views;

public partial class ScriptTabView : UserControl
{
    private ScriptTab? _tab;

    public ScriptTabView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ViewModels.ScriptTabViewModel vm)
                LoadTab(vm.Tab);
        };
    }

    public void LoadTab(ScriptTab tab)
    {
        _tab = tab;
        TabTitle.Text = tab.Name;
        RenderElements();
    }

    private void RenderElements()
    {
        ContentPanel.Children.Clear();
        if (_tab == null) return;

        var fg = (Brush)FindResource("TextBrush");
        var sub = (Brush)FindResource("SubTextBrush");
        var accent = (Brush)FindResource("AccentBrush");
        var control = (Brush)FindResource("ControlBrush");
        var stroke = (Brush)FindResource("StrokeBrush");

        foreach (var el in _tab.Elements)
        {
            switch (el.Type)
            {
                case UIElementType.Label:
                    var label = new TextBlock
                    {
                        Text = el.Text,
                        FontSize = el.FontSize,
                        FontWeight = el.Bold ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = fg,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 4)
                    };
                    ContentPanel.Children.Add(label);
                    break;

                case UIElementType.Button:
                    var btn = new Button
                    {
                        Content = el.Text,
                        Padding = new Thickness(16, 8, 16, 8),
                        Style = (Style)FindResource("AccentButtonStyle"),
                        Margin = new Thickness(0, 4, 0, 4),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    var callbackId = el.CallbackId;
                    btn.Click += (_, _) =>
                    {
                        if (_tab != null && _tab.Callbacks.TryGetValue(callbackId, out var cb))
                        {
                            try { cb(); }
                            catch (Exception ex)
                            {
                                NotificationService.Push("Script Error", ex.Message, NotificationType.Error);
                            }
                        }
                    };
                    ContentPanel.Children.Add(btn);
                    break;

                case UIElementType.TextInput:
                    var input = new TextBox
                    {
                        Style = (Style)FindResource("DarkTextBoxStyle"),
                        Tag = el.Placeholder,
                        FontSize = 13,
                        Margin = new Thickness(0, 4, 0, 4),
                        MaxWidth = 400,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    var inputId = el.Id;
                    input.TextChanged += (s, _) =>
                    {
                        if (_tab != null && s is TextBox tb)
                            ScriptUIService.SetState(_tab, inputId, tb.Text);
                    };
                    // Load persisted state
                    var existing = ScriptUIService.GetState(_tab, inputId);
                    if (!string.IsNullOrEmpty(existing))
                        input.Text = existing;
                    ContentPanel.Children.Add(input);
                    break;

                case UIElementType.Separator:
                    ContentPanel.Children.Add(new Border
                    {
                        Height = 1,
                        Background = stroke,
                        Margin = new Thickness(0, 10, 0, 10)
                    });
                    break;

                case UIElementType.ProgressBar:
                    var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
                    if (!string.IsNullOrEmpty(el.Text))
                    {
                        panel.Children.Add(new TextBlock
                        {
                            Text = el.Text,
                            FontSize = 12,
                            Foreground = sub,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                    }
                    var progress = new ProgressBar
                    {
                        Value = el.Value * 100,
                        Maximum = 100,
                        Height = 8,
                        MaxWidth = 400,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Foreground = accent,
                        Background = control
                    };
                    panel.Children.Add(progress);
                    ContentPanel.Children.Add(panel);
                    break;

                case UIElementType.CheckBox:
                    var check = new CheckBox
                    {
                        Content = el.Text,
                        IsChecked = el.IsChecked,
                        Foreground = fg,
                        FontSize = 13,
                        Margin = new Thickness(0, 4, 0, 4)
                    };
                    var checkId = el.Id;
                    check.Checked += (_, _) => { if (_tab != null) ScriptUIService.SetState(_tab, checkId, "true"); };
                    check.Unchecked += (_, _) => { if (_tab != null) ScriptUIService.SetState(_tab, checkId, "false"); };
                    ContentPanel.Children.Add(check);
                    break;

                case UIElementType.ComboBox:
                    var comboPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
                    if (!string.IsNullOrEmpty(el.Text))
                    {
                        comboPanel.Children.Add(new TextBlock
                        {
                            Text = el.Text,
                            FontSize = 12,
                            Foreground = sub,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                    }
                    var combo = new ComboBox
                    {
                        Style = (Style)FindResource("DarkComboBoxStyle"),
                        Width = 200,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    foreach (var opt in el.Options)
                        combo.Items.Add(opt);
                    if (el.SelectedIndex >= 0 && el.SelectedIndex < el.Options.Count)
                        combo.SelectedIndex = el.SelectedIndex;
                    var comboId = el.Id;
                    combo.SelectionChanged += (s, _) =>
                    {
                        if (_tab != null && s is ComboBox cb && cb.SelectedItem is string val)
                            ScriptUIService.SetState(_tab, comboId, val);
                    };
                    comboPanel.Children.Add(combo);
                    ContentPanel.Children.Add(comboPanel);
                    break;
            }
        }
    }
}
