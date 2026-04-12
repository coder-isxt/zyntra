using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class ScriptsView : UserControl
{
    private bool _suppressTextChanged;

    private static readonly SolidColorBrush KeywordBrush = new(Color.FromRgb(0xFF, 0x7B, 0x72));
    private static readonly SolidColorBrush StringBrush = new(Color.FromRgb(0xA5, 0xD6, 0xFF));
    private static readonly SolidColorBrush CommentBrush = new(Color.FromRgb(0x8B, 0x94, 0x9E));
    private static readonly SolidColorBrush NumberBrush = new(Color.FromRgb(0x79, 0xC0, 0xFF));
    private static readonly SolidColorBrush FuncBrush = new(Color.FromRgb(0xD2, 0xA8, 0xFF));
    private static readonly SolidColorBrush BuiltinBrush = new(Color.FromRgb(0xFF, 0xD6, 0x6E));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0xC9, 0xD1, 0xD9));

    private static readonly HashSet<string> Keywords = new()
    {
        "and", "break", "do", "else", "elseif", "end", "false", "for",
        "function", "goto", "if", "in", "local", "nil", "not", "or",
        "repeat", "return", "then", "true", "until", "while"
    };

    private static readonly HashSet<string> Builtins = new()
    {
        "print", "type", "tostring", "tonumber", "pairs", "ipairs",
        "require", "error", "assert", "pcall", "xpcall", "select",
        "unpack", "table", "string", "math", "io", "os", "zyntra"
    };

    private static readonly Regex TokenRegex = new(
        @"(?<comment>--\[\[[\s\S]*?\]\]|--[^\n]*)" +
        @"|(?<string>\[\[[\s\S]*?\]\]|""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*')" +
        @"|(?<number>\b\d+\.?\d*(?:[eE][+-]?\d+)?\b|0x[0-9a-fA-F]+)" +
        @"|(?<word>[a-zA-Z_]\w*)" +
        @"|(?<other>.)",
        RegexOptions.Compiled);

    public ScriptsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScriptsViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ScriptsViewModel.SelectedScript))
                    LoadEditorContent();
            };
            LoadEditorContent();
        }
    }

    private void LoadEditorContent()
    {
        var vm = DataContext as ScriptsViewModel;
        string text = vm?.EditorContent ?? string.Empty;
        SetEditorText(text);
    }

    private void SetEditorText(string text)
    {
        _suppressTextChanged = true;
        try
        {
            CodeEditor.Document.Blocks.Clear();
            var para = new Paragraph();
            ApplySyntaxHighlighting(para, text);
            CodeEditor.Document.Blocks.Add(para);
            UpdateLineNumbers(text);
        }
        finally
        {
            _suppressTextChanged = false;
        }
    }

    private void ApplySyntaxHighlighting(Paragraph para, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var matches = TokenRegex.Matches(text);
        foreach (Match m in matches)
        {
            SolidColorBrush brush;
            if (m.Groups["comment"].Success)
                brush = CommentBrush;
            else if (m.Groups["string"].Success)
                brush = StringBrush;
            else if (m.Groups["number"].Success)
                brush = NumberBrush;
            else if (m.Groups["word"].Success)
            {
                string word = m.Value;
                if (Keywords.Contains(word))
                    brush = KeywordBrush;
                else if (Builtins.Contains(word))
                    brush = BuiltinBrush;
                else
                    brush = DefaultBrush;
            }
            else
                brush = DefaultBrush;

            var run = new Run(m.Value) { Foreground = brush };
            if (m.Groups["word"].Success && Keywords.Contains(m.Value))
                run.FontWeight = FontWeights.Bold;
            para.Inlines.Add(run);
        }
    }

    private string GetEditorText()
    {
        var range = new TextRange(CodeEditor.Document.ContentStart, CodeEditor.Document.ContentEnd);
        string text = range.Text;
        // RichTextBox appends a trailing newline
        if (text.EndsWith("\r\n")) text = text[..^2];
        else if (text.EndsWith("\n")) text = text[..^1];
        return text;
    }

    private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged) return;

        string text = GetEditorText();

        // Update the view model
        if (DataContext is ScriptsViewModel vm && vm.EditorContent != text)
        {
            vm.EditorContent = text;
        }

        // Re-apply highlighting
        _suppressTextChanged = true;
        try
        {
            var caretOffset = CodeEditor.Document.ContentStart.GetOffsetToPosition(CodeEditor.CaretPosition);
            CodeEditor.Document.Blocks.Clear();
            var para = new Paragraph();
            ApplySyntaxHighlighting(para, text);
            CodeEditor.Document.Blocks.Add(para);

            // Restore caret
            var newPos = CodeEditor.Document.ContentStart.GetPositionAtOffset(caretOffset);
            if (newPos != null)
                CodeEditor.CaretPosition = newPos;
        }
        finally
        {
            _suppressTextChanged = false;
        }

        UpdateLineNumbers(text);
    }

    private void CodeEditor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            e.Handled = true;
            CodeEditor.CaretPosition.InsertTextInRun("    ");
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
        count = Math.Max(count, 15);
        var numbers = new List<string>(count);
        for (int i = 1; i <= count; i++)
            numbers.Add(i.ToString());
        LineNumbersPanel.ItemsSource = numbers;
    }
}
