using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Fracture.Models;
using Fracture.Services;
using Fracture.ViewModels;

namespace Fracture.Views;

public partial class ScriptsView : UserControl
{
    private CompletionWindow? _completionWindow;
    private ScriptsViewModel? _vm;

    public ScriptsView()
    {
        InitializeComponent();
        LoadLuaHighlighting();
        ConfigureEditor();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void LoadLuaHighlighting()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Fracture.Resources.Lua.xshd");
            if (stream == null)
                return;

            using var reader = new System.Xml.XmlTextReader(stream);
            var xshd = HighlightingLoader.LoadXshd(reader);
            CodeEditor.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load Lua highlighting: {ex}");
        }
    }

    private void ConfigureEditor()
    {
        CodeEditor.TextArea.TextEntering += OnTextEntering;
        CodeEditor.TextArea.TextEntered += OnTextEntered;
        CodeEditor.TextChanged += OnEditorTextChanged;

        // Enable search (Ctrl+F)
        SearchPanel.Install(CodeEditor);

        // Dark theme colors for editor
        CodeEditor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(25, 30, 40));
        CodeEditor.TextArea.TextView.CurrentLineBorder = null;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Initial sync from ViewModel to editor
        if (DataContext is ScriptsViewModel vm)
            SyncEditorFromViewModel(vm);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ScriptsViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            SyncEditorFromViewModel(vm);
        }
        else if (e.OldValue is ScriptsViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScriptsViewModel.EditorContent) && _vm != null)
        {
            // Only sync if the change came from outside (user didn't just type)
            if (CodeEditor.Text != _vm.EditorContent)
                SyncEditorFromViewModel(_vm);
        }
        else if (e.PropertyName == nameof(ScriptsViewModel.SelectedScript) && _vm != null)
        {
            SyncEditorFromViewModel(_vm);
        }
    }

    private void SyncEditorFromViewModel(ScriptsViewModel vm)
    {
        var cursor = CodeEditor.CaretOffset;
        CodeEditor.TextChanged -= OnEditorTextChanged;
        CodeEditor.Text = vm.EditorContent;
        if (cursor <= CodeEditor.Text.Length)
            CodeEditor.CaretOffset = cursor;
        CodeEditor.TextChanged += OnEditorTextChanged;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_vm != null && CodeEditor.Text != _vm.EditorContent)
            _vm.EditorContent = CodeEditor.Text;
    }

    private void OnTextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == ".")
        {
            _completionWindow = new CompletionWindow(CodeEditor.TextArea)
            {
                Width = 320,
                MaxHeight = 200,
                Background = new SolidColorBrush(Color.FromRgb(21, 24, 31)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 63, 80)),
                BorderThickness = new Thickness(1),
            };

            // Style the list box
            var listBox = _completionWindow.CompletionList.ListBox;
            listBox.Background = new SolidColorBrush(Color.FromRgb(21, 24, 31));
            listBox.BorderThickness = new Thickness(0);

            var items = LuaCompletion.Filter(CodeEditor.TextArea.Document.GetText(0, CodeEditor.CaretOffset));
            foreach (var item in items)
                _completionWindow.CompletionList.CompletionData.Add(item);

            if (_completionWindow.CompletionList.CompletionData.Count > 0)
            {
                _completionWindow.Show();
                _completionWindow.Closed += (o, args) => _completionWindow = null;
            }
            else
            {
                _completionWindow.Close();
            }
        }
    }

    private void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == " ")
        {
            _completionWindow?.Close();
        }
    }

    private void OnClearOutputClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScriptsViewModel vm)
            vm.Output = string.Empty;
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
}
