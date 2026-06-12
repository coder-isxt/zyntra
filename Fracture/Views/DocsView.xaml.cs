using System.Windows;
using System.Windows.Controls;
using Fracture.ViewModels;
using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;

namespace Fracture.Views;

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

        var activeStyle = (Style)FindResource("PillButtonActiveStyle");
        var normalStyle = (Style)FindResource("PillButtonStyle");

        BtnOverview.Style = tag == "overview" ? activeStyle : normalStyle;
        BtnLuaApi.Style = tag == "lua" ? activeStyle : normalStyle;
        BtnScriptUI.Style = tag == "scriptui" ? activeStyle : normalStyle;

        switch (tag)
        {
            case "lua": ShowLuaApi(); break;
            case "scriptui": ShowScriptUI(); break;
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
            Background = (Brush)FindResource("CardBrush"), CornerRadius = new CornerRadius(8),
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
            Background = (Brush)FindResource("AccentSoftBrush"), CornerRadius = new CornerRadius(0, 8, 8, 0),
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

    private void AddFeatureCard(string icon, string title, string description)
    {
        var border = new Border
        {
            Background = (Brush)FindResource("CardBrush"), CornerRadius = new CornerRadius(12),
            BorderBrush = Stroke, BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 14, 16, 14),
            Margin = new Thickness(0, 0, 0, 8),
            Effect = (System.Windows.Media.Effects.Effect)FindResource("CardShadow"),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconHolder = new Border
        {
            Width = 40, Height = 40, CornerRadius = new CornerRadius(10),
            Background = (Brush)FindResource("AccentSoftBrush"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var iconBlock = new TextBlock
        {
            Text = icon, FontSize = 18, VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
        };
        iconHolder.Child = iconBlock;
        Grid.SetColumn(iconHolder, 0);
        grid.Children.Add(iconHolder);

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = Fg,
        });
        stack.Children.Add(new TextBlock
        {
            Text = description, FontSize = 12, Foreground = Sub, Opacity = 0.8,
            Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetColumn(stack, 2);
        grid.Children.Add(stack);

        border.Child = grid;
        DocPanel.Children.Add(border);
    }

    private void AddStepRow(string number, string text)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var badge = new Border
        {
            Background = (Brush)FindResource("AccentGradientBrush"), CornerRadius = new CornerRadius(10),
            Width = 22, Height = 22, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 1, 0, 0),
        };
        badge.Child = new TextBlock
        {
            Text = number, FontSize = 10, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
        };
        Grid.SetColumn(badge, 0);
        grid.Children.Add(badge);

        var desc = new TextBlock
        {
            Text = text, FontSize = 13, Foreground = Sub,
            TextWrapping = TextWrapping.Wrap, LineHeight = 21,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(desc, 2);
        grid.Children.Add(desc);

        DocPanel.Children.Add(grid);
    }

    // ── Pages ───────────────────────────────────────────────

    private void ShowOverview()
    {
        Clear();
        AddTitle("Fracture Scripting API");
        AddSubtitle("Automate everything with Lua. Fracture injects the full API into every script — launch games, manage accounts, send notifications, and more.");

        AddHeading("How It Works");
        AddStepRow("1", "When you run a script, Fracture injects your accounts, apps, and recent games as native Lua tables.");
        AddStepRow("2", "The fracture API module is loaded automatically — no setup or imports needed.");
        AddStepRow("3", "Actions like game launches, notifications, and clipboard are executed after your script finishes.");

        AddDivider();
        AddHeading("What You Can Do");
        AddFeatureCard("\U0001F3AE", "Launch Games", "Join Roblox games with any account, mass-launch for all accounts or by tag.");
        AddFeatureCard("\U0001F465", "Manage Accounts", "Query accounts, filter by tag, get display names and usernames.");
        AddFeatureCard("\U0001F514", "Send Notifications", "Push info, success, warning, or error notifications to the Fracture panel.");
        AddFeatureCard("\U0001F4CB", "Clipboard", "Copy text to the Windows clipboard after script completes.");
        AddFeatureCard("\U0001F504", "Recently Played", "Access your recent game history and rejoin with one call.");
        AddFeatureCard("\U0001F4E6", "App Access", "Query all registered applications and their paths.");

        AddDivider();
        AddHeading("Quick Start");

        AddCodeBlock("Launch a game", @"fracture.launch_game(""MyAccount"", 4483381587)
fracture.notify(""Launched"", ""Joining game!"", ""Success"")", "LUA");

        AddCodeBlock("List all accounts", @"for _, acc in ipairs(fracture.get_accounts()) do
    fracture.log(acc.DisplayName .. "" ["" .. acc.Tag .. ""]"")
end", "LUA");

        AddCodeBlock("Mass launch all alts", @"fracture.launch_game_all(4483381587, ""alt"")
fracture.notify(""Mass Launch"", ""All alts joining!"", ""Success"")", "LUA");

        AddNote("See the Lua API Reference tab for the complete function list with all parameters and more examples.");
    }

    private void ShowLuaApi()
    {
        Clear();
        AddTitle("Lua API Reference");
        AddSubtitle("All functions are available via the global fracture table. No imports needed.");

        // Context
        AddHeading("Context");
        AddFunctionRow("fracture.get_version()", "Returns the Fracture version string");
        AddFunctionRow("fracture.get_data_dir()", "Returns the Fracture AppData directory path");

        // Accounts
        AddHeading("Accounts");
        AddFunctionRow("fracture.get_accounts()", "Returns all Roblox accounts as a table");
        AddFunctionRow("fracture.get_account(name)", "Find account by username or display name");
        AddFunctionRow("fracture.get_accounts_by_tag(tag)", "Filter accounts by tag");
        AddFunctionRow("fracture.get_account_count()", "Returns the number of accounts");
        AddNote("Fields: UserId, Username, DisplayName, Tag, CookieValid");

        // Apps
        AddHeading("Apps");
        AddFunctionRow("fracture.get_apps()", "Returns all registered applications");
        AddFunctionRow("fracture.get_app(name)", "Find an app by name");
        AddFunctionRow("fracture.get_app_count()", "Returns the number of apps");
        AddNote("Fields: Id, Name, ExePath, Description, IsGameModule");

        // Game Launch
        AddHeading("Game Launch");
        AddFunctionRow("fracture.launch_game(account_name, place_id)", "Launch a Roblox game with a specific account");
        AddFunctionRow("fracture.launch_game_all(place_id, tag)", "Launch for all accounts, optionally filtered by tag");
        AddNote("Launches are queued and executed after the script finishes. Fracture decrypts the cookie and launches Roblox automatically.");

        // Recently Played
        AddHeading("Recently Played");
        AddFunctionRow("fracture.get_recently_played()", "Returns all recently played games");
        AddFunctionRow("fracture.get_last_played()", "Returns the most recent game, or nil");
        AddNote("Fields: PlaceId, GameName, AccountName, PlayedAt");

        // Notifications
        AddHeading("Notifications");
        AddFunctionRow("fracture.notify(title, message, type)", "Send a notification to Fracture");
        AddNote("Type: \"Info\" (default), \"Success\", \"Warning\", \"Error\"");

        // Clipboard
        AddHeading("Clipboard");
        AddFunctionRow("fracture.set_clipboard(text)", "Copy text to the Windows clipboard");

        // Utilities
        AddHeading("Utilities");
        AddFunctionRow("fracture.log(message)", "Writes a timestamped log line to the output panel");
        AddFunctionRow("fracture.sleep(ms)", "Pauses execution for the given milliseconds");

        // Examples
        AddDivider();
        AddHeading("Examples");

        AddCodeBlock("Launch a game with one account", @"fracture.launch_game(""MyAccount"", 4483381587)
fracture.notify(""Launching"", ""Joining game as MyAccount"")", "LUA");

        AddCodeBlock("Mass launch all alt accounts", @"fracture.launch_game_all(4483381587, ""alt"")
local alts = fracture.get_accounts_by_tag(""alt"")
fracture.notify(""Mass Launch"", #alts .. "" alts joining"", ""Success"")", "LUA");

        AddCodeBlock("Rejoin the last played game", @"local last = fracture.get_last_played()
if last then
    fracture.launch_game(last.AccountName, last.PlaceId)
    fracture.log(""Rejoining "" .. last.GameName)
else
    fracture.log(""No recent games"")
end", "LUA");

        AddCodeBlock("List all accounts with tags", @"for _, acc in ipairs(fracture.get_accounts()) do
    fracture.log(acc.DisplayName .. "" ["" .. acc.Tag .. ""]"")
end
fracture.notify(""Done"", ""Listed "" .. fracture.get_account_count() .. "" accounts"", ""Success"")", "LUA");

        AddCodeBlock("Copy account names to clipboard", @"local names = {}
for _, acc in ipairs(fracture.get_accounts()) do
    table.insert(names, acc.DisplayName)
end
fracture.set_clipboard(table.concat(names, "", ""))
fracture.log(""Copied "" .. #names .. "" names to clipboard"")", "LUA");

        AddCodeBlock("Queue all accounts into a game", @"local accounts = fracture.get_accounts()
for i, acc in ipairs(accounts) do
    fracture.launch_game(acc.Username, 4483381587)
    fracture.log(""Queued "" .. acc.DisplayName .. "" ("" .. i .. ""/"" .. #accounts .. "")"")
end
fracture.notify(""Done"", #accounts .. "" accounts queued"", ""Success"")", "LUA");
    }

    private void ShowScriptUI()
    {
        Clear();
        AddTitle("Script UI API");
        AddSubtitle("Create custom tabs with interactive UI elements from Lua scripts.");

        AddHeading("Getting Started");
        AddStepRow("1", "Create a tab with fracture.ui.create_tab(name, icon)");
        AddStepRow("2", "Add elements: labels, buttons, inputs, checkboxes, dropdowns, progress bars");
        AddStepRow("3", "Run the script — the tab appears in the sidebar automatically");
        AddStepRow("4", "Button callbacks execute when clicked by the user");

        AddDivider();
        AddHeading("fracture.ui Functions");
        AddFunctionRow("create_tab(name, icon)", "Creates a new sidebar tab. Returns a tab handle. Icon is optional (e.g. \"\u2605\").");
        AddFunctionRow("add_label(tab, text, opts)", "Adds a text label. opts: {font_size=14, bold=false}");
        AddFunctionRow("add_button(tab, text, callback)", "Adds a clickable button. callback is a Lua function.");
        AddFunctionRow("add_text_input(tab, id, opts)", "Adds a text input field. opts: {placeholder=\"...\"}");
        AddFunctionRow("add_separator(tab)", "Adds a horizontal divider line.");
        AddFunctionRow("add_progress(tab, value, label)", "Adds a progress bar. value is 0.0\u20131.0.");
        AddFunctionRow("add_checkbox(tab, id, text, checked)", "Adds a checkbox. checked is a boolean.");
        AddFunctionRow("add_dropdown(tab, id, label, options, index)", "Adds a dropdown. options is a table of strings.");
        AddFunctionRow("get_value(tab, id)", "Gets the current value of an input, checkbox, or dropdown by id.");

        AddDivider();
        AddHeading("Example: Custom Dashboard");
        AddCodeBlock("my_dashboard.lua", @"local tab = fracture.ui.create_tab(""My Dashboard"", ""★"")

fracture.ui.add_label(tab, ""Account Manager"", {font_size = 20, bold = true})
fracture.ui.add_separator(tab)

fracture.ui.add_label(tab, ""You have "" .. fracture.get_account_count() .. "" accounts"")

fracture.ui.add_text_input(tab, ""place_id"", {placeholder = ""Enter Place ID...""})

fracture.ui.add_button(tab, ""▶ Launch All"", function()
    local pid = fracture.ui.get_value(tab, ""place_id"")
    if pid ~= """" then
        fracture.launch_game_all(tonumber(pid))
        fracture.notify(""Launched"", ""All accounts joining "" .. pid, ""Success"")
    end
end)

fracture.ui.add_separator(tab)
fracture.ui.add_checkbox(tab, ""auto_refresh"", ""Auto-refresh cookies"", true)
fracture.ui.add_dropdown(tab, ""server"", ""Region"", {""US East"", ""US West"", ""Europe"", ""Asia""}, 1)", "LUA");

        AddDivider();
        AddHeading("How It Works");
        AddStepRow("1", "Running the script creates a sidebar tab visible to the user.");
        AddStepRow("2", "UI elements are rendered in the tab with the app\u2019s dark theme.");
        AddStepRow("3", "Button callbacks run the Lua function when the user clicks.");
        AddStepRow("4", "get_value() reads input/checkbox/dropdown state at any time.");
        AddStepRow("5", "Re-running the script replaces the tab with the updated layout.");

        AddNote("Script tabs persist in the sidebar until the app is restarted or the script creates a new version of the same tab.");
    }
}
