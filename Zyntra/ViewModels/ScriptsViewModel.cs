using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Zyntra.Models;
using Zyntra.Services;

namespace Zyntra.ViewModels;

public class ScriptsViewModel : BaseViewModel
{
    public ObservableCollection<ScriptEntry> Scripts { get; } = new();

    private ScriptEntry? _selectedScript;
    public ScriptEntry? SelectedScript
    {
        get => _selectedScript;
        set
        {
            if (SetProperty(ref _selectedScript, value))
            {
                OnPropertyChanged(nameof(EditorName));
                OnPropertyChanged(nameof(EditorContent));
                OnPropertyChanged(nameof(EditorType));
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(SchedulerEnabled));
                OnPropertyChanged(nameof(SchedulerInterval));
            }
        }
    }

    public string EditorName
    {
        get => _selectedScript?.Name ?? string.Empty;
        set { if (_selectedScript != null) { _selectedScript.Name = value; OnPropertyChanged(); } }
    }

    public string EditorContent
    {
        get => _selectedScript?.Content ?? string.Empty;
        set { if (_selectedScript != null) { _selectedScript.Content = value; OnPropertyChanged(); } }
    }

    public string EditorType
    {
        get => _selectedScript?.ScriptType ?? "Lua";
        set { if (_selectedScript != null) { _selectedScript.ScriptType = value; OnPropertyChanged(); } }
    }

    public bool HasSelection => _selectedScript != null;

    public bool SchedulerEnabled
    {
        get => _selectedScript?.SchedulerEnabled ?? false;
        set
        {
            if (_selectedScript != null)
            {
                _selectedScript.SchedulerEnabled = value;
                if (value && _selectedScript.NextScheduledRun == null)
                    _selectedScript.NextScheduledRun = DateTime.UtcNow.AddMinutes(_selectedScript.SchedulerIntervalMinutes);
                OnPropertyChanged();
                SaveScripts();
            }
        }
    }

    public string SchedulerInterval
    {
        get => (_selectedScript?.SchedulerIntervalMinutes ?? 60).ToString();
        set
        {
            if (_selectedScript != null && int.TryParse(value, out int mins) && mins > 0)
            {
                _selectedScript.SchedulerIntervalMinutes = mins;
                if (_selectedScript.SchedulerEnabled)
                    _selectedScript.NextScheduledRun = DateTime.UtcNow.AddMinutes(mins);
                OnPropertyChanged();
                SaveScripts();
            }
        }
    }

    private string _output = string.Empty;
    public string Output
    {
        get => _output;
        set => SetProperty(ref _output, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand NewScriptCommand { get; }
    public ICommand DeleteScriptCommand { get; }
    public ICommand RunScriptCommand { get; }
    public ICommand SaveScriptsCommand { get; }

    public static string[] ScriptTypes => new[] { "Lua" };

    public ScriptsViewModel()
    {
        NewScriptCommand = new RelayCommand(_ => NewScript());
        DeleteScriptCommand = new RelayCommand(_ => DeleteScript());
        RunScriptCommand = new RelayCommand(async _ => await RunScript());
        SaveScriptsCommand = new RelayCommand(_ => SaveScripts());

        LoadScripts();
    }

    private void LoadScripts()
    {
        Scripts.Clear();
        bool needsSave = false;
        foreach (var s in ScriptService.Load())
        {
            if (s.ScriptType != "Lua")
            {
                s.ScriptType = "Lua";
                needsSave = true;
            }
            Scripts.Add(s);
        }
        if (needsSave) SaveScripts();
        StatusText = $"{Scripts.Count} script(s)";
    }

    private void NewScript()
    {
        var settings = SettingsService.Load();
        var script = new ScriptEntry
        {
            Name = "New Script",
            ScriptType = "Lua",
            Content = settings.DefaultScriptTemplate,
        };
        Scripts.Add(script);
        SelectedScript = script;
        SaveScripts();
    }

    public void DuplicateScript(ScriptEntry source)
    {
        var copy = new ScriptEntry
        {
            Name = source.Name + " (copy)",
            ScriptType = source.ScriptType,
            Content = source.Content,
            SchedulerEnabled = false,
            SchedulerIntervalMinutes = source.SchedulerIntervalMinutes,
        };
        Scripts.Add(copy);
        SelectedScript = copy;
        SaveScripts();
    }

    private void DeleteScript()
    {
        if (_selectedScript == null) return;
        Scripts.Remove(_selectedScript);
        SelectedScript = Scripts.FirstOrDefault();
        SaveScripts();
    }

    private async Task RunScript()
    {
        if (_selectedScript == null) return;
        IsRunning = true;
        Output = "Running...\n";
        StatusText = $"Running {_selectedScript.Name}...";

        try
        {
            string result = await ScriptService.RunAsync(_selectedScript);
            Output = result;
            StatusText = $"Finished {_selectedScript.Name}";
            NotificationService.Push("Script Complete", $"{_selectedScript.Name} finished.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            Output = $"Error: {ex.Message}";
            StatusText = "Script failed";
            NotificationService.Push("Script Error", ex.Message, NotificationType.Error);
        }
        finally
        {
            IsRunning = false;
            SaveScripts(); // Persist LastRunAt
        }
    }

    private void SaveScripts()
    {
        ScriptService.Save(Scripts.ToList());
    }
}
