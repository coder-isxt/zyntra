using System.Windows;
using System.Windows.Controls;
using Zyntra.ViewModels;
using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;

namespace Zyntra.Views;

public partial class DocsView : UserControl
{
    public DocsView()
    {
        InitializeComponent();
        ShowOverview();
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

        switch (tag)
        {
            case "powershell": ShowPowerShell(); break;
            case "python": ShowPython(); break;
            case "batch": ShowBatch(); break;
            default: ShowOverview(); break;
        }
    }

    // ── Helpers ─────────────────────────────────────────────

    private Brush Fg => (Brush)FindResource("TextBrush");
    private Brush Sub => (Brush)FindResource("SubTextBrush");
    private Brush Accent => (Brush)FindResource("AccentBrush");
    private Brush Inset => (Brush)FindResource("PanelInsetBrush");
    private Brush Stroke => (Brush)FindResource("StrokeBrush");
    private Brush CodeBg => new SolidColorBrush(Color.FromRgb(13, 17, 23));

    private void Clear() => DocPanel.Children.Clear();

    private void AddTitle(string text)
    {
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 22, FontWeight = FontWeights.Bold,
            Foreground = Fg, Margin = new Thickness(0, 0, 0, 4),
        });
    }

    private void AddSubtitle(string text)
    {
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 13, Foreground = Sub,
            Margin = new Thickness(0, 0, 0, 16), TextWrapping = TextWrapping.Wrap,
        });
    }

    private void AddHeading(string text)
    {
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 15, FontWeight = FontWeights.SemiBold,
            Foreground = Accent, Margin = new Thickness(0, 18, 0, 8),
        });
    }

    private void AddParagraph(string text)
    {
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 13, Foreground = Fg,
            TextWrapping = TextWrapping.Wrap, LineHeight = 21,
            Margin = new Thickness(0, 0, 0, 8),
        });
    }

    private void AddFunctionRow(string name, string desc)
    {
        var row = new Border
        {
            Background = Inset, CornerRadius = new CornerRadius(6),
            BorderBrush = Stroke, BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 4),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var fnText = new TextBlock
        {
            Text = name, FontFamily = new FontFamily("Consolas"), FontSize = 12,
            Foreground = Accent, VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(fnText, 0);
        grid.Children.Add(fnText);

        var descText = new TextBlock
        {
            Text = desc, FontSize = 12, Foreground = Sub,
            VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetColumn(descText, 2);
        grid.Children.Add(descText);

        var copyBtn = MakeCopyButton(name);
        Grid.SetColumn(copyBtn, 4);
        grid.Children.Add(copyBtn);

        row.Child = grid;
        DocPanel.Children.Add(row);
    }

    private void AddCodeBlock(string title, string code)
    {
        if (!string.IsNullOrEmpty(title))
        {
            DocPanel.Children.Add(new TextBlock
            {
                Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = Fg, Margin = new Thickness(0, 14, 0, 6),
            });
        }

        var border = new Border
        {
            Background = CodeBg, CornerRadius = new CornerRadius(8),
            BorderBrush = Stroke, BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var copyBtn = MakeCopyButton(code.Trim());
        copyBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
        copyBtn.Margin = new Thickness(0, 0, 0, 6);
        Grid.SetRow(copyBtn, 0);
        grid.Children.Add(copyBtn);

        var codeText = new TextBlock
        {
            Text = code.Trim(), FontFamily = new FontFamily("Consolas"), FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(201, 209, 217)),
            TextWrapping = TextWrapping.Wrap, LineHeight = 20,
        };
        Grid.SetRow(codeText, 1);
        grid.Children.Add(codeText);

        border.Child = grid;
        DocPanel.Children.Add(border);
    }

    private void AddNote(string text)
    {
        var border = new Border
        {
            Background = Inset, CornerRadius = new CornerRadius(6),
            BorderBrush = Accent, BorderThickness = new Thickness(2, 0, 0, 0),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 6, 0, 10),
        };
        border.Child = new TextBlock
        {
            Text = text, FontSize = 12, Foreground = Sub,
            TextWrapping = TextWrapping.Wrap, LineHeight = 20,
        };
        DocPanel.Children.Add(border);
    }

    private Button MakeCopyButton(string textToCopy)
    {
        var btn = new Button
        {
            Content = "Copy", FontSize = 10, Padding = new Thickness(10, 3, 10, 3),
            Style = (Style)FindResource("ControlButtonStyle"),
            VerticalAlignment = VerticalAlignment.Center, Cursor = System.Windows.Input.Cursors.Hand,
        };
        btn.Click += (_, _) =>
        {
            System.Windows.Clipboard.SetText(textToCopy);
            btn.Content = "Copied!";
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (_, _) => { btn.Content = "Copy"; timer.Stop(); };
            timer.Start();
        };
        return btn;
    }

    private void AddSpacer(double height = 6)
    {
        DocPanel.Children.Add(new Border { Height = height });
    }

    // ── Pages ───────────────────────────────────────────────

    private void ShowOverview()
    {
        Clear();
        AddTitle("Zyntra Scripting API");
        AddSubtitle("Zyntra injects a scripting API into every script you run, giving your scripts access to accounts, apps, notifications, and more.");

        AddHeading("Supported Languages");
        AddFunctionRow("PowerShell", "API module auto-imported as ZyntraAPI");
        AddFunctionRow("Python", "API module auto-imported as 'zyntra'");
        AddFunctionRow("Batch", "Context JSON path via %ZYNTRA_CONTEXT%");

        AddHeading("How It Works");
        AddParagraph("1.  When you run a script, Zyntra exports a context JSON with your accounts, apps, and settings.");
        AddParagraph("2.  The API module is automatically injected into your script.");
        AddParagraph("3.  After execution, Zyntra reads a response file for notifications or clipboard actions.");

        AddNote("Tip: Use the PowerShell or Python tabs for the full API reference with copyable examples.");

        AddHeading("Quick Example (PowerShell)");
        AddCodeBlock("", @"$accounts = Get-ZyntraAccounts
foreach ($acc in $accounts) {
    Write-ZyntraLog ""$($acc.DisplayName) [$($acc.Tag)]""
}
Send-ZyntraNotification -Title 'Done' -Message 'Finished!' -Type Success");

        AddHeading("Quick Example (Python)");
        AddCodeBlock("", @"for acc in zyntra.get_accounts():
    zyntra.log(f""{acc['DisplayName']} [{acc.get('Tag', 'none')}]"")

zyntra.send_notification('Done', 'Finished!', 'Success')");
    }

    private void ShowPowerShell()
    {
        Clear();
        AddTitle("PowerShell API Reference");
        AddSubtitle("The ZyntraAPI module is auto-imported into every PowerShell script. All functions are available immediately.");

        AddHeading("Context");
        AddFunctionRow("Get-ZyntraVersion", "Returns the Zyntra version string");
        AddFunctionRow("Get-ZyntraDataDir", "Returns the Zyntra AppData directory path");

        AddHeading("Accounts");
        AddFunctionRow("Get-ZyntraAccounts", "Returns all Roblox accounts");
        AddFunctionRow("Get-ZyntraAccount -Name X", "Find account by username or display name");
        AddFunctionRow("Get-ZyntraAccountsByTag -Tag X", "Filter accounts by tag");
        AddFunctionRow("Get-ZyntraAccountCount", "Returns the number of accounts");
        AddNote("Account properties: UserId, Username, DisplayName, Tag, CookieValid");

        AddHeading("Apps");
        AddFunctionRow("Get-ZyntraApps", "Returns all registered applications");
        AddFunctionRow("Get-ZyntraApp -Name X", "Find an app by name");
        AddFunctionRow("Get-ZyntraAppCount", "Returns the number of apps");
        AddNote("App properties: Id, Name, ExePath, Description, IsGameModule");

        AddHeading("Notifications");
        AddFunctionRow("Send-ZyntraNotification -Title X -Message Y [-Type Z]", "Send a notification to Zyntra");
        AddNote("Type options: Info (default), Success, Warning, Error");

        AddHeading("Clipboard");
        AddFunctionRow("Set-ZyntraClipboard -Text X", "Sets the Windows clipboard after script completes");

        AddHeading("Utilities");
        AddFunctionRow("Write-ZyntraLog -Message X", "Writes a timestamped log line to output");

        AddHeading("Examples");
        AddCodeBlock("List all accounts with tags", @"$accounts = Get-ZyntraAccounts
foreach ($acc in $accounts) {
    Write-ZyntraLog ""$($acc.DisplayName) [$($acc.Tag)]""
}
Send-ZyntraNotification -Title 'Done' `
    -Message ""Listed $($accounts.Count) accounts"" `
    -Type Success");

        AddCodeBlock("Copy account names to clipboard", @"$names = (Get-ZyntraAccounts).DisplayName -join ', '
Set-ZyntraClipboard -Text $names
Write-ZyntraLog ""Copied $((Get-ZyntraAccounts).Count) names to clipboard""");

        AddCodeBlock("Check which accounts have valid cookies", @"$accounts = Get-ZyntraAccounts
$valid = ($accounts | Where-Object { $_.CookieValid -eq $true }).Count
$invalid = ($accounts | Where-Object { $_.CookieValid -eq $false }).Count
Write-ZyntraLog ""Valid: $valid | Invalid: $invalid""
Send-ZyntraNotification -Title 'Health Check' `
    -Message ""$valid valid, $invalid invalid cookies"" `
    -Type $(if ($invalid -gt 0) { 'Warning' } else { 'Success' })");

        AddCodeBlock("Launch an app by name", @"$app = Get-ZyntraApp -Name 'Potassium'
if ($app) {
    Start-Process $app.ExePath
    Write-ZyntraLog ""Launched $($app.Name)""
} else {
    Write-ZyntraLog 'App not found'
}");
    }

    private void ShowPython()
    {
        Clear();
        AddTitle("Python API Reference");
        AddSubtitle("The zyntra_api module is auto-imported as 'zyntra'. All functions are available via zyntra.function_name().");

        AddHeading("Context");
        AddFunctionRow("zyntra.get_version()", "Returns the Zyntra version string");
        AddFunctionRow("zyntra.get_data_dir()", "Returns the Zyntra AppData directory path");

        AddHeading("Accounts");
        AddFunctionRow("zyntra.get_accounts()", "Returns all accounts as list of dicts");
        AddFunctionRow("zyntra.get_account(name)", "Find account by username or display name");
        AddFunctionRow("zyntra.get_accounts_by_tag(tag)", "Filter accounts by tag");
        AddFunctionRow("zyntra.get_account_count()", "Returns the number of accounts");
        AddNote("Dict keys: UserId, Username, DisplayName, Tag, CookieValid");

        AddHeading("Apps");
        AddFunctionRow("zyntra.get_apps()", "Returns all apps as list of dicts");
        AddFunctionRow("zyntra.get_app(name)", "Find an app by name");
        AddFunctionRow("zyntra.get_app_count()", "Returns the number of apps");
        AddNote("Dict keys: Id, Name, ExePath, Description, IsGameModule");

        AddHeading("Notifications");
        AddFunctionRow("zyntra.send_notification(title, message, type='Info')", "Send a notification to Zyntra");
        AddNote("type options: 'Info' (default), 'Success', 'Warning', 'Error'");

        AddHeading("Clipboard");
        AddFunctionRow("zyntra.set_clipboard(text)", "Sets the Windows clipboard after script completes");

        AddHeading("Utilities");
        AddFunctionRow("zyntra.log(message)", "Writes a timestamped log line to output");

        AddHeading("Examples");
        AddCodeBlock("List all accounts", @"for acc in zyntra.get_accounts():
    zyntra.log(f""{acc['DisplayName']} [{acc.get('Tag', 'none')}]"")

zyntra.send_notification(
    'Done',
    f'Listed {zyntra.get_account_count()} accounts',
    'Success'
)");

        AddCodeBlock("Export app names to clipboard", @"names = ', '.join(a['Name'] for a in zyntra.get_apps())
zyntra.set_clipboard(names)
zyntra.log(f'Copied {zyntra.get_app_count()} app names')");

        AddCodeBlock("Filter accounts by tag", @"alts = zyntra.get_accounts_by_tag('alt')
zyntra.log(f'Found {len(alts)} alt accounts:')
for acc in alts:
    zyntra.log(f'  - {acc[""DisplayName""]}')");

        AddCodeBlock("Launch an app by name", @"import subprocess
app = zyntra.get_app('Potassium')
if app:
    subprocess.Popen(app['ExePath'])
    zyntra.log(f""Launched {app['Name']}"")
else:
    zyntra.log('App not found')");
    }

    private void ShowBatch()
    {
        Clear();
        AddTitle("Batch Script Notes");
        AddSubtitle("Batch scripts have limited API support. The context JSON path is available via the ZYNTRA_CONTEXT environment variable.");

        AddNote("For full API access with functions and response handling, use PowerShell or Python instead.");

        AddHeading("Reading Context from Batch");
        AddParagraph("You can shell out to PowerShell to read the context JSON:");

        AddCodeBlock("Read Zyntra version", @"for /f ""delims="" %%v in ('powershell -NoProfile -Command ^
    ""(Get-Content '%ZYNTRA_CONTEXT%' | ConvertFrom-Json).Version""') do (
    echo Zyntra version: %%v
)");

        AddCodeBlock("Count accounts", @"for /f ""delims="" %%c in ('powershell -NoProfile -Command ^
    ""(Get-Content '%ZYNTRA_CONTEXT%' | ConvertFrom-Json).Accounts.Count""') do (
    echo Accounts: %%c
)");

        AddHeading("Environment Variable");
        AddFunctionRow("%ZYNTRA_CONTEXT%", "Full path to the context JSON file");

        AddHeading("Context JSON Structure");
        AddCodeBlock("", @"{
  ""Version"": ""1.0.7"",
  ""DataDir"": ""C:\\Users\\...\\AppData\\Roaming\\Zyntra"",
  ""ResponseFile"": ""C:\\...\\zyntra_response.json"",
  ""Accounts"": [
    { ""UserId"": ""123"", ""Username"": ""user"", ""DisplayName"": ""User"", ""Tag"": ""main"" }
  ],
  ""Apps"": [
    { ""Id"": ""..."", ""Name"": ""MyApp"", ""ExePath"": ""C:\\..."" }
  ]
}");
    }
}
