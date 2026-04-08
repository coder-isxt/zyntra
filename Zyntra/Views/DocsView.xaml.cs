using System.Windows;
using System.Windows.Controls;
using Zyntra.ViewModels;

namespace Zyntra.Views;

public partial class DocsView : UserControl
{
    public DocsView()
    {
        InitializeComponent();
        DocContent.Text = DocsViewModel.OverviewDoc.Trim();
    }

    private void OnDocNav(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        string tag = btn.Tag as string ?? "overview";

        var activeStyle = (Style)FindResource("SidebarActiveButtonStyle");
        var normalStyle = (Style)FindResource("SidebarButtonStyle");

        BtnOverview.Style = tag == "overview" ? activeStyle : normalStyle;
        BtnPowerShell.Style = tag == "powershell" ? activeStyle : normalStyle;
        BtnPython.Style = tag == "python" ? activeStyle : normalStyle;
        BtnBatch.Style = tag == "batch" ? activeStyle : normalStyle;

        DocContent.Text = tag switch
        {
            "powershell" => DocsViewModel.PowerShellDoc.Trim(),
            "python" => DocsViewModel.PythonDoc.Trim(),
            "batch" => DocsViewModel.BatchDoc.Trim(),
            _ => DocsViewModel.OverviewDoc.Trim(),
        };
    }
}
