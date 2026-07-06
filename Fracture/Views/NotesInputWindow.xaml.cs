using System.Windows;

namespace Fracture.Views;

public partial class NotesInputWindow : Window
{
    public string NotesResult { get; private set; } = string.Empty;

    public NotesInputWindow(string username = "", string currentNote = "")
    {
        InitializeComponent();
        TitleText.Text = string.IsNullOrEmpty(username) ? "Account Note" : $"Note — {username}";
        NotesInput.Text = currentNote;
        NotesInput.Focus();
        NotesInput.CaretIndex = NotesInput.Text.Length;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        NotesResult = NotesInput.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        NotesResult = string.Empty;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
