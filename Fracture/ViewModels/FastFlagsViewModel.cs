using System.Collections.ObjectModel;
using System.Windows.Input;
using Fracture.Services;

namespace Fracture.ViewModels;

public class FastFlagPresetVM : BaseViewModel
{
    private readonly FastFlagsViewModel _owner;
    public string Key { get; }
    public string Value { get; }
    public string Description { get; }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                _owner.OnPresetToggled(this);
        }
    }

    public FastFlagPresetVM(FastFlagsViewModel owner, FastFlagService.FastFlagPreset preset, bool enabled)
    {
        _owner = owner;
        Key = preset.Key;
        Value = preset.Value;
        Description = preset.Description;
        _isEnabled = enabled;
    }
}

public class FastFlagsViewModel : BaseViewModel
{
    private bool _suppressPresetSync;

    public ObservableCollection<FastFlagPresetVM> Presets { get; } = new();

    private string _flagsJson = "{}";
    public string FlagsJson
    {
        get => _flagsJson;
        set => SetProperty(ref _flagsJson, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ClearCommand { get; }

    public FastFlagsViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save());
        ReloadCommand = new RelayCommand(_ => Reload());
        ClearCommand = new RelayCommand(_ => Clear());
        Reload();
    }

    private void Reload()
    {
        var flags = FastFlagService.Load();
        FlagsJson = FastFlagService.ToJson(flags);
        BuildPresets(flags);

        var folders = FastFlagService.GetVersionFolders();
        StatusText = folders.Count == 0
            ? "No Roblox installation detected — set the Roblox folder in Settings"
            : $"Loaded {flags.Count} flag(s) — {folders.Count} Roblox version folder(s) detected";
    }

    private void BuildPresets(Dictionary<string, string> flags)
    {
        _suppressPresetSync = true;
        Presets.Clear();
        foreach (var preset in FastFlagService.CommonPresets)
        {
            bool enabled = flags.ContainsKey(preset.Key);
            Presets.Add(new FastFlagPresetVM(this, preset, enabled));
        }
        _suppressPresetSync = false;
    }

    public void OnPresetToggled(FastFlagPresetVM preset)
    {
        if (_suppressPresetSync) return;

        Dictionary<string, string> flags;
        try
        {
            flags = FastFlagService.Parse(FlagsJson);
        }
        catch
        {
            StatusText = "Fix the JSON before toggling presets";
            return;
        }

        if (preset.IsEnabled)
            flags[preset.Key] = preset.Value;
        else
            flags.Remove(preset.Key);

        FlagsJson = FastFlagService.ToJson(flags);
        StatusText = preset.IsEnabled ? $"Enabled {preset.Key}" : $"Disabled {preset.Key}";
    }

    private void Save()
    {
        Dictionary<string, string> flags;
        try
        {
            flags = FastFlagService.Parse(FlagsJson);
        }
        catch (Exception ex)
        {
            StatusText = $"Invalid JSON: {ex.Message}";
            System.Windows.MessageBox.Show($"The FastFlags JSON is invalid:\n\n{ex.Message}",
                "Fracture", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        int written = FastFlagService.Save(flags);
        FlagsJson = FastFlagService.ToJson(flags);
        BuildPresets(flags);

        StatusText = written > 0
            ? $"Saved {flags.Count} flag(s) to {written} Roblox version folder(s)"
            : "No Roblox version folders found to write to";
    }

    private void Clear()
    {
        FlagsJson = "{}";
        FastFlagService.Save(new Dictionary<string, string>());
        BuildPresets(new Dictionary<string, string>());
        StatusText = "Cleared all FastFlags";
    }
}
