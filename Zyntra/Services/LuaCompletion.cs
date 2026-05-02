using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Zyntra.Services;

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

        // ── zyntra core API ──
        New("zyntra", "Zyntra script API root namespace", "zyntra"),
        New("zyntra.log", "zyntra.log(message) — Log to the script output", "zyntra"),
        New("zyntra.notify", "zyntra.notify(title, message, type?) — Show a notification", "zyntra"),
        New("zyntra.sleep", "zyntra.sleep(ms) — Pause script execution", "zyntra"),
        New("zyntra.set_clipboard", "zyntra.set_clipboard(text) — Copy text to clipboard", "zyntra"),

        New("zyntra.get_version", "zyntra.get_version() — Returns Zyntra version string", "zyntra"),
        New("zyntra.get_data_dir", "zyntra.get_data_dir() — Path to Zyntra's data folder", "zyntra"),

        // ── zyntra accounts ──
        New("zyntra.get_accounts", "zyntra.get_accounts() — Returns all Roblox accounts", "zyntra"),
        New("zyntra.get_account", "zyntra.get_account(name) — Get account by username/display name", "zyntra"),
        New("zyntra.get_accounts_by_tag", "zyntra.get_accounts_by_tag(tag) — Filter accounts by tag", "zyntra"),
        New("zyntra.get_account_count", "zyntra.get_account_count() — Number of accounts", "zyntra"),

        // ── zyntra apps ──
        New("zyntra.get_apps", "zyntra.get_apps() — Returns all configured apps", "zyntra"),
        New("zyntra.get_app", "zyntra.get_app(name) — Get app by name", "zyntra"),
        New("zyntra.get_app_count", "zyntra.get_app_count() — Number of apps", "zyntra"),

        // ── zyntra games ──
        New("zyntra.launch_game", "zyntra.launch_game(account_name, place_id?) — Launch Roblox", "zyntra"),
        New("zyntra.launch_game_all", "zyntra.launch_game_all(place_id, tag?) — Launch on all (or tagged) accounts", "zyntra"),
        New("zyntra.get_recently_played", "zyntra.get_recently_played() — Recently played games", "zyntra"),
        New("zyntra.get_last_played", "zyntra.get_last_played() — Most recent game", "zyntra"),

        // ── zyntra.ui ──
        New("zyntra.ui", "Custom UI tab/element API", "zyntra"),
        New("zyntra.ui.create_tab", "zyntra.ui.create_tab(name, icon?) — Create a sidebar tab", "zyntra"),
        New("zyntra.ui.add_label", "zyntra.ui.add_label(tab, text, opts?) — opts: font_size, bold", "zyntra"),
        New("zyntra.ui.add_button", "zyntra.ui.add_button(tab, text, callback)", "zyntra"),
        New("zyntra.ui.add_text_input", "zyntra.ui.add_text_input(tab, id, opts?) — opts: placeholder", "zyntra"),
        New("zyntra.ui.add_checkbox", "zyntra.ui.add_checkbox(tab, id, text, checked?)", "zyntra"),
        New("zyntra.ui.add_dropdown", "zyntra.ui.add_dropdown(tab, id, label, options, selected?)", "zyntra"),
        New("zyntra.ui.add_progress", "zyntra.ui.add_progress(tab, value, label?)", "zyntra"),
        New("zyntra.ui.add_separator", "zyntra.ui.add_separator(tab)", "zyntra"),
        New("zyntra.ui.get_value", "zyntra.ui.get_value(tab, id) — Read input/checkbox/dropdown", "zyntra"),
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
