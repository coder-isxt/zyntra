using System.Collections.ObjectModel;

namespace Zyntra.Services;

public enum UIElementType
{
    Label,
    Button,
    TextInput,
    Separator,
    ProgressBar,
    Image,
    CheckBox,
    ComboBox
}

public class ScriptUIElement
{
    public UIElementType Type { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double FontSize { get; set; } = 14;
    public bool Bold { get; set; }
    public string Placeholder { get; set; } = string.Empty;
    public double Value { get; set; }
    public bool IsChecked { get; set; }
    public List<string> Options { get; set; } = new();
    public int SelectedIndex { get; set; }
    public string CallbackId { get; set; } = string.Empty;
}

public class ScriptTab
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "◇";
    public string ScriptId { get; set; } = string.Empty;
    public List<ScriptUIElement> Elements { get; set; } = new();
    public Dictionary<string, Action> Callbacks { get; set; } = new();
    public Dictionary<string, string> State { get; set; } = new();
}

public static class ScriptUIService
{
    public static ObservableCollection<ScriptTab> Tabs { get; } = new();

    private static readonly object _lock = new();

    public static ScriptTab CreateTab(string name, string icon, string scriptId)
    {
        var tab = new ScriptTab
        {
            Name = name,
            Icon = string.IsNullOrEmpty(icon) ? "◇" : icon,
            ScriptId = scriptId
        };

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                // Remove existing tab from same script with same name
                var existing = Tabs.FirstOrDefault(t => t.ScriptId == scriptId && t.Name == name);
                if (existing != null)
                    Tabs.Remove(existing);
                Tabs.Add(tab);
            }
        });

        OnChanged?.Invoke();
        return tab;
    }

    public static void AddElement(ScriptTab tab, ScriptUIElement element)
    {
        lock (_lock)
        {
            tab.Elements.Add(element);
        }
    }

    public static void RegisterCallback(ScriptTab tab, string callbackId, Action callback)
    {
        lock (_lock)
        {
            tab.Callbacks[callbackId] = callback;
        }
    }

    public static void SetState(ScriptTab tab, string key, string value)
    {
        lock (_lock)
        {
            tab.State[key] = value;
        }
    }

    public static string GetState(ScriptTab tab, string key)
    {
        lock (_lock)
        {
            return tab.State.TryGetValue(key, out var val) ? val : string.Empty;
        }
    }

    public static void RemoveTabsForScript(string scriptId)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                var toRemove = Tabs.Where(t => t.ScriptId == scriptId).ToList();
                foreach (var t in toRemove)
                    Tabs.Remove(t);
            }
        });
        OnChanged?.Invoke();
    }

    public static void Clear()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                Tabs.Clear();
            }
        });
        OnChanged?.Invoke();
    }

    public static event Action? OnChanged;
}
