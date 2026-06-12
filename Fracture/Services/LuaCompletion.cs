using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Fracture.Services;

public class LuaCompletionData : ICompletionData
{
    public LuaCompletionData(string text, string description, string category)
    {
        Text = text;
        Description = description;
        Category = category;
    }

    public ImageSource? Image => null;
    public string Text { get; }
    public string Category { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority => 1;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}

public static class LuaCompletion
{
    public static readonly LuaCompletionData[] All = new[]
    {
        // ── Lua keywords ──
        New("and", "Logical AND operator", "keyword"),
        New("break", "Break out of a loop", "keyword"),
        New("do", "Block start", "keyword"),
        New("else", "Else branch", "keyword"),
        New("elseif", "Else-if branch", "keyword"),
        New("end", "Block end", "keyword"),
        New("for", "For loop", "keyword"),
        New("function", "Function definition", "keyword"),
        New("if", "If statement", "keyword"),
        New("in", "For-in iterator", "keyword"),
        New("local", "Local variable declaration", "keyword"),
        New("not", "Logical NOT operator", "keyword"),
        New("or", "Logical OR operator", "keyword"),
        New("repeat", "Repeat loop", "keyword"),
        New("return", "Return from function", "keyword"),
        New("then", "Then clause of if", "keyword"),
        New("until", "Until clause of repeat", "keyword"),
        New("while", "While loop", "keyword"),
        New("true", "Boolean true", "keyword"),
        New("false", "Boolean false", "keyword"),
        New("nil", "Nil value", "keyword"),

        // ── Lua builtins ──
        New("print", "print(...) — Print to stdout", "builtin"),
        New("tostring", "tostring(value) — Convert to string", "builtin"),
        New("tonumber", "tonumber(value) — Convert to number", "builtin"),
        New("type", "type(value) — Get the type of a value", "builtin"),
        New("ipairs", "ipairs(t) — Iterate over array part", "builtin"),
        New("pairs", "pairs(t) — Iterate over all key-value pairs", "builtin"),
        New("pcall", "pcall(f, ...) — Protected call", "builtin"),
        New("table.insert", "table.insert(t, [pos,] value)", "builtin"),
        New("table.remove", "table.remove(t, [pos])", "builtin"),
        New("table.concat", "table.concat(t, sep)", "builtin"),
        New("string.format", "string.format(fmt, ...)", "builtin"),
        New("string.sub", "string.sub(s, i, j)", "builtin"),
        New("string.upper", "string.upper(s)", "builtin"),
        New("string.lower", "string.lower(s)", "builtin"),
        New("math.floor", "math.floor(x)", "builtin"),
        New("math.ceil", "math.ceil(x)", "builtin"),
        New("math.random", "math.random([m,] [n])", "builtin"),

        // ── fracture core API ──
        New("fracture", "Fracture script API root namespace", "fracture"),
        New("fracture.log", "fracture.log(message) — Log to the script output", "fracture"),
        New("fracture.notify", "fracture.notify(title, message, type?) — Show a notification", "fracture"),
        New("fracture.sleep", "fracture.sleep(ms) — Pause script execution", "fracture"),
        New("fracture.set_clipboard", "fracture.set_clipboard(text) — Copy text to clipboard", "fracture"),

        New("fracture.get_version", "fracture.get_version() — Returns Fracture version string", "fracture"),
        New("fracture.get_data_dir", "fracture.get_data_dir() — Path to Fracture's data folder", "fracture"),

        // ── fracture accounts ──
        New("fracture.get_accounts", "fracture.get_accounts() — Returns all Roblox accounts", "fracture"),
        New("fracture.get_account", "fracture.get_account(name) — Get account by username/display name", "fracture"),
        New("fracture.get_accounts_by_tag", "fracture.get_accounts_by_tag(tag) — Filter accounts by tag", "fracture"),
        New("fracture.get_account_count", "fracture.get_account_count() — Number of accounts", "fracture"),

        // ── fracture apps ──
        New("fracture.get_apps", "fracture.get_apps() — Returns all configured apps", "fracture"),
        New("fracture.get_app", "fracture.get_app(name) — Get app by name", "fracture"),
        New("fracture.get_app_count", "fracture.get_app_count() — Number of apps", "fracture"),

        // ── fracture games ──
        New("fracture.launch_game", "fracture.launch_game(account_name, place_id?) — Launch Roblox", "fracture"),
        New("fracture.launch_game_all", "fracture.launch_game_all(place_id, tag?) — Launch on all (or tagged) accounts", "fracture"),
        New("fracture.get_recently_played", "fracture.get_recently_played() — Recently played games", "fracture"),
        New("fracture.get_last_played", "fracture.get_last_played() — Most recent game", "fracture"),

        // ── fracture.ui ──
        New("fracture.ui", "Custom UI tab/element API", "fracture"),
        New("fracture.ui.create_tab", "fracture.ui.create_tab(name, icon?) — Create a sidebar tab", "fracture"),
        New("fracture.ui.add_label", "fracture.ui.add_label(tab, text, opts?) — opts: font_size, bold", "fracture"),
        New("fracture.ui.add_button", "fracture.ui.add_button(tab, text, callback)", "fracture"),
        New("fracture.ui.add_text_input", "fracture.ui.add_text_input(tab, id, opts?) — opts: placeholder", "fracture"),
        New("fracture.ui.add_checkbox", "fracture.ui.add_checkbox(tab, id, text, checked?)", "fracture"),
        New("fracture.ui.add_dropdown", "fracture.ui.add_dropdown(tab, id, label, options, selected?)", "fracture"),
        New("fracture.ui.add_progress", "fracture.ui.add_progress(tab, value, label?)", "fracture"),
        New("fracture.ui.add_separator", "fracture.ui.add_separator(tab)", "fracture"),
        New("fracture.ui.get_value", "fracture.ui.get_value(tab, id) — Read input/checkbox/dropdown", "fracture"),
    };

    private static LuaCompletionData New(string text, string description, string category)
        => new(text, description, category);

    /// <summary>
    /// Filters completions by prefix (case-insensitive). If the prefix contains a dot,
    /// returns only items that share that exact namespace.
    /// </summary>
    public static IEnumerable<LuaCompletionData> Filter(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return All;

        return All.Where(c => c.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
