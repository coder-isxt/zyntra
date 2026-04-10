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
        BtnLuaApi.Style = tag == "lua" ? activeStyle : normalStyle;
        BtnPlugins.Style = tag == "plugins" ? activeStyle : normalStyle;

        switch (tag)
        {
            case "lua": ShowLuaApi(); break;
            case "plugins": ShowPlugins(); break;
            default: ShowOverview(); break;
        }
    }

    // ── Helpers ─────────────────────────────────────────────

    private Brush Fg => (Brush)FindResource("TextBrush");
    private Brush Sub => (Brush)FindResource("SubTextBrush");
    private Brush Accent => (Brush)FindResource("AccentBrush");
    private Brush Panel => (Brush)FindResource("PanelBrush");
    private Brush Stroke => (Brush)FindResource("StrokeBrush");
    private Brush Control => (Brush)FindResource("ControlBrush");
    private Brush CodeBg => new SolidColorBrush(Color.FromRgb(13, 17, 23));
    private Brush CodeFg => new SolidColorBrush(Color.FromRgb(201, 209, 217));

    private void Clear() => DocPanel.Children.Clear();

    private void AddTitle(string text)
    {
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 24, FontWeight = FontWeights.Bold,
            Foreground = Fg, Margin = new Thickness(0, 0, 0, 6),
        });
    }

    private void AddSubtitle(string text)
    {
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 14, Foreground = Sub,
            Margin = new Thickness(0, 0, 0, 20), TextWrapping = TextWrapping.Wrap,
            LineHeight = 22,
        });
    }

    private void AddHeading(string text)
    {
        DocPanel.Children.Add(new Border { Height = 12 }); // spacer above
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 16, FontWeight = FontWeights.SemiBold,
            Foreground = Fg, Margin = new Thickness(0, 0, 0, 10),
        });
    }

    private void AddParagraph(string text)
    {
        DocPanel.Children.Add(new TextBlock
        {
            Text = text, FontSize = 13, Foreground = Sub,
            TextWrapping = TextWrapping.Wrap, LineHeight = 22,
            Margin = new Thickness(0, 0, 0, 10),
        });
    }

    private void AddFunctionRow(string name, string desc)
    {
        var row = new Border
        {
            Background = Panel, CornerRadius = new CornerRadius(6),
            BorderBrush = Stroke, BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 6),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var fnText = new TextBlock
        {
            Text = name, FontFamily = new FontFamily("Consolas"), FontSize = 12.5,
            Foreground = Accent, VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(fnText, 0);
        grid.Children.Add(fnText);

        var descText = new TextBlock
        {
            Text = desc, FontSize = 12, Foreground = Sub,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetColumn(descText, 2);
        grid.Children.Add(descText);

        var copyBtn = MakeCopyButton(name, "Copy");
        Grid.SetColumn(copyBtn, 4);
        grid.Children.Add(copyBtn);

        row.Child = grid;
        DocPanel.Children.Add(row);
    }

    private void AddCodeBlock(string title, string code, string lang = "")
    {
        if (!string.IsNullOrEmpty(title))
        {
            DocPanel.Children.Add(new TextBlock
            {
                Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = Fg, Margin = new Thickness(0, 16, 0, 8),
            });
        }

        var border = new Border
        {
            Background = CodeBg, CornerRadius = new CornerRadius(8),
            BorderBrush = Stroke, BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 12),
        };

        var outerGrid = new Grid();
        outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
        outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header row: language label + copy button
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (!string.IsNullOrEmpty(lang))
        {
            var langLabel = new TextBlock
            {
                Text = lang, FontSize = 10, FontWeight = FontWeights.SemiBold,
                Foreground = Sub, Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(langLabel, 0);
            headerGrid.Children.Add(langLabel);
        }

        var copyBtn = MakeCopyButton(code.Trim(), "Copy code");
        Grid.SetColumn(copyBtn, 1);
        headerGrid.Children.Add(copyBtn);

        Grid.SetRow(headerGrid, 0);
        outerGrid.Children.Add(headerGrid);

        var codeText = new TextBlock
        {
            Text = code.Trim(), FontFamily = new FontFamily("Consolas"), FontSize = 12.5,
            Foreground = CodeFg, TextWrapping = TextWrapping.Wrap, LineHeight = 21,
        };
        Grid.SetRow(codeText, 2);
        outerGrid.Children.Add(codeText);

        border.Child = outerGrid;
        DocPanel.Children.Add(border);
    }

    private void AddNote(string text)
    {
        var border = new Border
        {
            Background = Panel, CornerRadius = new CornerRadius(0, 6, 6, 0),
            BorderBrush = Accent, BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 8, 0, 12),
        };
        border.Child = new TextBlock
        {
            Text = text, FontSize = 12.5, Foreground = Sub,
            TextWrapping = TextWrapping.Wrap, LineHeight = 21,
        };
        DocPanel.Children.Add(border);
    }

    private void AddDivider()
    {
        DocPanel.Children.Add(new Border
        {
            BorderBrush = Stroke, BorderThickness = new Thickness(0, 0, 0, 1),
            Margin = new Thickness(0, 16, 0, 8),
        });
    }

    private Button MakeCopyButton(string textToCopy, string label = "Copy")
    {
        var btn = new Button
        {
            Content = label, FontSize = 10, Padding = new Thickness(12, 4, 12, 4),
            Style = (Style)FindResource("ControlButtonStyle"),
            VerticalAlignment = VerticalAlignment.Center, Cursor = System.Windows.Input.Cursors.Hand,
        };
        string origLabel = label;
        btn.Click += (_, _) =>
        {
            System.Windows.Clipboard.SetText(textToCopy);
            btn.Content = "Copied!";
            btn.Foreground = Accent;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (_, _) => { btn.Content = origLabel; btn.ClearValue(System.Windows.Controls.Control.ForegroundProperty); timer.Stop(); };
            timer.Start();
        };
        return btn;
    }

    private void AddSpacer(double height = 8)
    {
        DocPanel.Children.Add(new Border { Height = height });
    }

    // ── Pages ───────────────────────────────────────────────

    private void ShowOverview()
    {
        Clear();
        AddTitle("Zyntra Scripting API");
        AddSubtitle("Zyntra uses Lua as its scripting language. The zyntra API is auto-injected into every script, giving you access to accounts, apps, game launching, and more.");

        AddHeading("How It Works");
        AddParagraph("1.  When you run a script, Zyntra injects your accounts, apps, and recent games as native Lua tables.");
        AddParagraph("2.  The zyntra API module is loaded automatically — no setup needed.");
        AddParagraph("3.  Actions like game launches, notifications, and clipboard are executed after your script finishes.");

        AddNote("Select Lua API from the sidebar for the full function reference with copyable examples.");

        AddDivider();
        AddHeading("Quick Example — Launch a Game");
        AddCodeBlock("", @"-- Launch a game with a specific account
zyntra.launch_game(""MyAccount"", 4483381587)
zyntra.notify(""Launched"", ""Joining game!"", ""Success"")", "LUA");

        AddHeading("Quick Example — List Accounts");
        AddCodeBlock("", @"for _, acc in ipairs(zyntra.get_accounts()) do
    zyntra.log(acc.DisplayName .. "" ["" .. acc.Tag .. ""]"")
end
zyntra.notify(""Done"", ""Finished!"", ""Success"")", "LUA");

        AddHeading("Quick Example — Mass Launch");
        AddCodeBlock("", @"-- Launch a game for all accounts tagged 'alt'
zyntra.launch_game_all(4483381587, ""alt"")
zyntra.notify(""Mass Launch"", ""All alts joining!"", ""Success"")", "LUA");
    }

    private void ShowLuaApi()
    {
        Clear();
        AddTitle("Lua API Reference");
        AddSubtitle("The zyntra module is auto-loaded into every Lua script. All functions are available via zyntra.function_name().");

        AddHeading("Context");
        AddFunctionRow("zyntra.get_version()", "Returns the Zyntra version string");
        AddFunctionRow("zyntra.get_data_dir()", "Returns the Zyntra AppData directory path");

        AddHeading("Accounts");
        AddFunctionRow("zyntra.get_accounts()", "Returns all Roblox accounts as a table");
        AddFunctionRow("zyntra.get_account(name)", "Find account by username or display name");
        AddFunctionRow("zyntra.get_accounts_by_tag(tag)", "Filter accounts by tag");
        AddFunctionRow("zyntra.get_account_count()", "Returns the number of accounts");
        AddNote("Account fields: UserId, Username, DisplayName, Tag, CookieValid");

        AddHeading("Apps");
        AddFunctionRow("zyntra.get_apps()", "Returns all registered applications");
        AddFunctionRow("zyntra.get_app(name)", "Find an app by name");
        AddFunctionRow("zyntra.get_app_count()", "Returns the number of apps");
        AddNote("App fields: Id, Name, ExePath, Description, IsGameModule");

        AddHeading("Notifications");
        AddFunctionRow(@"zyntra.notify(title, message, type)", "Send a notification to Zyntra");
        AddNote(@"type options: ""Info"" (default), ""Success"", ""Warning"", ""Error""");

        AddHeading("Clipboard");
        AddFunctionRow("zyntra.set_clipboard(text)", "Sets the Windows clipboard after script completes");

        AddHeading("Game Launch");
        AddFunctionRow(@"zyntra.launch_game(account_name, place_id)", "Launch a Roblox game with a specific account");
        AddFunctionRow(@"zyntra.launch_game_all(place_id, tag)", "Launch a game for all accounts (optionally filtered by tag)");
        AddNote("Games are launched after the script finishes. Zyntra resolves the account, decrypts the cookie, and launches Roblox automatically.");

        AddHeading("Recently Played");
        AddFunctionRow("zyntra.get_recently_played()", "Returns all recently played games");
        AddFunctionRow("zyntra.get_last_played()", "Returns the most recently played game, or nil");
        AddNote("Game fields: PlaceId, GameName, AccountName, PlayedAt");

        AddHeading("Utilities");
        AddFunctionRow("zyntra.log(message)", "Writes a timestamped log line to output");
        AddFunctionRow("zyntra.sleep(ms)", "Pauses execution for the given milliseconds");

        AddDivider();
        AddHeading("Examples");

        AddCodeBlock("Launch a game with one account", @"zyntra.launch_game(""MyAccount"", 4483381587)
zyntra.notify(""Launching"", ""Joining game as MyAccount"")", "LUA");

        AddCodeBlock("Launch a game for all alt accounts", @"zyntra.launch_game_all(4483381587, ""alt"")
local alts = zyntra.get_accounts_by_tag(""alt"")
zyntra.notify(""Mass Launch"", #alts .. "" alts joining"", ""Success"")", "LUA");

        AddCodeBlock("Rejoin the last played game", @"local last = zyntra.get_last_played()
if last then
    zyntra.launch_game(last.AccountName, last.PlaceId)
    zyntra.log(""Rejoining "" .. last.GameName)
else
    zyntra.log(""No recent games"")
end", "LUA");

        AddCodeBlock("List all accounts with tags", @"for _, acc in ipairs(zyntra.get_accounts()) do
    zyntra.log(acc.DisplayName .. "" ["" .. acc.Tag .. ""]"")
end
zyntra.notify(""Done"", ""Listed "" .. zyntra.get_account_count() .. "" accounts"", ""Success"")", "LUA");

        AddCodeBlock("Copy account names to clipboard", @"local names = {}
for _, acc in ipairs(zyntra.get_accounts()) do
    table.insert(names, acc.DisplayName)
end
zyntra.set_clipboard(table.concat(names, "", ""))
zyntra.log(""Copied "" .. #names .. "" names to clipboard"")", "LUA");

        AddCodeBlock("Launch with delay between accounts", @"local accounts = zyntra.get_accounts()
for i, acc in ipairs(accounts) do
    zyntra.launch_game(acc.Username, 4483381587)
    zyntra.log(""Queued "" .. acc.DisplayName .. "" ("" .. i .. ""/"" .. #accounts .. "")"")
end
zyntra.notify(""Done"", #accounts .. "" accounts queued"", ""Success"")", "LUA");
    }

    private void ShowPlugins()
    {
        Clear();
        AddTitle("Plugin SDK");
        AddSubtitle("Extend Zyntra with custom plugins. Plugins are .NET class libraries that implement the IZyntraPlugin interface.");

        AddHeading("Getting Started");
        AddParagraph("1.  Create a .NET class library project targeting the same framework as Zyntra.");
        AddParagraph("2.  Reference the IZyntraPlugin interface (or copy it into your project).");
        AddParagraph("3.  Implement the interface in a public class.");
        AddParagraph("4.  Build and install the DLL via the Plugins tab in Zyntra.");

        AddHeading("IZyntraPlugin Interface");
        AddCodeBlock("", @"public interface IZyntraPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    void Initialize();
    void Execute();
    void Shutdown();
}", "C#");

        AddHeading("Interface Members");
        AddFunctionRow("Name", "Display name shown in the Plugins list");
        AddFunctionRow("Description", "Short description of what the plugin does");
        AddFunctionRow("Version", "Version string (e.g. \"1.0.0\")");
        AddFunctionRow("Initialize()", "Called when the plugin is loaded at startup");
        AddFunctionRow("Execute()", "Called when the plugin is triggered to run");
        AddFunctionRow("Shutdown()", "Called when Zyntra is closing or the plugin is disabled");

        AddDivider();
        AddHeading("Example Plugin");
        AddCodeBlock("MyPlugin.cs", @"using Zyntra.Services;

public class MyPlugin : IZyntraPlugin
{
    public string Name => ""My Plugin"";
    public string Description => ""A sample Zyntra plugin"";
    public string Version => ""1.0.0"";

    public void Initialize()
    {
        // Called once when plugin loads
    }

    public void Execute()
    {
        // Your plugin logic here
        NotificationService.Push(
            ""My Plugin"",
            ""Plugin executed successfully!"",
            NotificationType.Success
        );
    }

    public void Shutdown()
    {
        // Cleanup resources
    }
}", "C#");

        AddHeading("Plugin Lifecycle");
        AddParagraph("1.  Install — User selects a .DLL file from the Plugins tab. Zyntra copies it to the plugins folder and reads metadata.");
        AddParagraph("2.  Enable — The plugin is loaded and Initialize() is called on application startup.");
        AddParagraph("3.  Execute — Execute() can be called by Zyntra when triggered.");
        AddParagraph("4.  Shutdown — Shutdown() is called when the app closes or the plugin is disabled.");

        AddNote("Plugins run in the same process as Zyntra. Be careful with exceptions — unhandled errors will be caught, but may cause the plugin to be marked as failed.");

        AddDivider();
        AddHeading("File Locations");
        AddFunctionRow("Plugin DLLs", "%AppData%\\Zyntra\\plugins\\");
        AddFunctionRow("Plugin index", "%AppData%\\Zyntra\\plugins.json");

        AddHeading("Tips");
        AddNote("Keep plugins lightweight. Avoid blocking the UI thread in Initialize() or Execute(). Use async patterns or background threads for long-running operations.");
    }
}
