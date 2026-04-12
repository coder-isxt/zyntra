using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Zyntra.Views;

public partial class ScriptsView : UserControl
{
    private static readonly SolidColorBrush KeywordBrush = new(Color.FromRgb(0xFF, 0x7B, 0x72));
    private static readonly SolidColorBrush StringBrush = new(Color.FromRgb(0xA5, 0xD6, 0xFF));
    private static readonly SolidColorBrush CommentBrush = new(Color.FromRgb(0x8B, 0x94, 0x9E));
    private static readonly SolidColorBrush NumberBrush = new(Color.FromRgb(0x79, 0xC0, 0xFF));
    private static readonly SolidColorBrush BuiltinBrush = new(Color.FromRgb(0xFF, 0xD6, 0x6E));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0xC9, 0xD1, 0xD9));

    private static readonly HashSet<string> LuaKeywords = new()
    {
        "and", "break", "do", "else", "elseif", "end", "false", "for",
        "function", "goto", "if", "in", "local", "nil", "not", "or",
        "repeat", "return", "then", "true", "until", "while"
    };

    private static readonly HashSet<string> LuaBuiltins = new()
    {
        "print", "type", "tostring", "tonumber", "pairs", "ipairs",
        "require", "error", "assert", "pcall", "xpcall", "select",
        "unpack", "table", "string", "math", "io", "os", "zyntra"
    };

    private static readonly Regex TokenRegex = new(
        @"(--\[\[[\s\S]*?\]\])" +             // multi-line comment
        @"|(--[^\n]*)" +                       // single-line comment
        @"|(\[\[[\s\S]*?\]\])" +               // multi-line string
        @"|(""(?:[^""\\]|\\.)*"")" +            // double-quoted string
        @"|('(?:[^'\\]|\\.)*')" +              // single-quoted string
        @"|(\b\d+\.?\d*(?:[eE][+-]?\d+)?\b)" + // numbers
        @"|(0x[0-9a-fA-F]+)" +                // hex numbers
        @"|([a-zA-Z_]\w*)",                    // identifiers
        RegexOptions.Compiled);

    public ScriptsView()
    {
        InitializeComponent();
        UpdateLineNumbers("");
    }

    private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb)
        {
            UpdateLineNumbers(tb.Text);
            UpdateHighlightLayer(tb.Text);
        }
    }

    private void CodeEditor_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        LineNumberScroll.ScrollToVerticalOffset(e.VerticalOffset);
        HighlightScroll.ScrollToVerticalOffset(e.VerticalOffset);
        HighlightScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
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

    private void UpdateHighlightLayer(string text)
    {
        HighlightLayer.Document.Blocks.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        // Process line by line so newlines become proper LineBreaks
        var lines = text.Split('\n');
        var para = new Paragraph();

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                para.Inlines.Add(new LineBreak());

            string line = lines[i].TrimEnd('\r');
            if (line.Length == 0)
                continue;

            int pos = 0;
            var matches = TokenRegex.Matches(line);
            foreach (Match m in matches)
            {
                // Add any skipped characters as default
                if (m.Index > pos)
                {
                    para.Inlines.Add(new Run(line[pos..m.Index]) { Foreground = DefaultBrush });
                }

                SolidColorBrush brush;
                bool bold = false;

                if (m.Groups[1].Success || m.Groups[2].Success) // comments
                    brush = CommentBrush;
                else if (m.Groups[3].Success || m.Groups[4].Success || m.Groups[5].Success) // strings
                    brush = StringBrush;
                else if (m.Groups[6].Success || m.Groups[7].Success) // numbers
                    brush = NumberBrush;
                else if (m.Groups[8].Success) // identifiers
                {
                    if (LuaKeywords.Contains(m.Value))
                    {
                        brush = KeywordBrush;
                        bold = true;
                    }
                    else if (LuaBuiltins.Contains(m.Value))
                        brush = BuiltinBrush;
                    else
                        brush = DefaultBrush;
                }
                else
                    brush = DefaultBrush;

                var run = new Run(m.Value) { Foreground = brush };
                if (bold) run.FontWeight = FontWeights.Bold;
                para.Inlines.Add(run);

                pos = m.Index + m.Length;
            }

            // Trailing text
            if (pos < line.Length)
                para.Inlines.Add(new Run(line[pos..]) { Foreground = DefaultBrush });
        }

        HighlightLayer.Document.Blocks.Add(para);
    }
}
