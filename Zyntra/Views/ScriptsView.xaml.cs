using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class ScriptsView : UserControl
{
    public ScriptsView()
    {
        InitializeComponent();
        UpdateLineNumbers("");
    }

    private void OnContextRunScript(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is ScriptEntry script && DataContext is ScriptsViewModel vm)
        {
            vm.SelectedScript = script;
            vm.RunScriptCommand.Execute(null);
        }
    }

    private void OnContextDuplicateScript(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is ScriptEntry script && DataContext is ScriptsViewModel vm)
            vm.DuplicateScript(script);
    }

    private void OnContextDeleteScript(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is ScriptEntry script && DataContext is ScriptsViewModel vm)
        {
            vm.SelectedScript = script;
            vm.DeleteScriptCommand.Execute(null);
        }
    }

    private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb)
            UpdateLineNumbers(tb.Text);
    }

    private void CodeEditor_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        LineNumberScroll.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void CodeEditor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Tab && sender is System.Windows.Controls.TextBox tb)
        {
            e.Handled = true;
            int caret = tb.CaretIndex;
            tb.Text = tb.Text.Insert(caret, "    ");
            tb.CaretIndex = caret + 4;
        }
    }

    private void OnClearOutputClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScriptsViewModel vm)
            vm.Output = string.Empty;
    }

    private void UpdateLineNumbers(string text)
    {
        int count = 1;
        if (!string.IsNullOrEmpty(text))
        {
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') count++;
        }
        count = Math.Max(count, 20);
        var lines = new string[count];
        for (int i = 0; i < count; i++)
            lines[i] = (i + 1).ToString();
        LineNumbers.Text = string.Join("\n", lines);
    }
}
