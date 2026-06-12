using System.Windows;
using System.Windows.Input;

namespace Fracture.Views;

public partial class TagInputWindow : Window
{
    public string TagResult { get; private set; } = string.Empty;

    public TagInputWindow(string currentTag = "")
    {
        InitializeComponent();
        TagInput.Text = currentTag;
        TagInput.Focus();
        TagInput.SelectAll();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        TagResult = TagInput.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        TagResult = string.Empty;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
