using System.Windows;
using System.Windows.Controls;

namespace Zyntra.Views;

public partial class ScriptsView : UserControl
{
    public ScriptsView()
    {
        InitializeComponent();
        UpdateLineNumbers("");
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

    private void UpdateLineNumbers(string text)
    {
        int count = 1;
        if (!string.IsNullOrEmpty(text))
        {
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') count++;
        }
        // Minimum 20 lines shown for visual padding
        count = Math.Max(count, 20);
        var lines = new string[count];
        for (int i = 0; i < count; i++)
            lines[i] = (i + 1).ToString();
        LineNumbers.Text = string.Join("\n", lines);
    }
}
