using System.Windows.Input;

namespace Zyntra.ViewModels;

public class YouTubePlayerViewModel : BaseViewModel
{
    private string _videoInput = string.Empty;
    public string VideoInput
    {
        get => _videoInput;
        set => SetProperty(ref _videoInput, value);
    }

    private string _currentVideoId = string.Empty;
    public string CurrentVideoId
    {
        get => _currentVideoId;
        set => SetProperty(ref _currentVideoId, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand LoadVideoCommand { get; }
    public ICommand OpenPipCommand { get; }
    public ICommand StopCommand { get; }

    public event Action? LoadRequested;
    public event Action? PipRequested;
    public event Action? StopRequested;

    public YouTubePlayerViewModel()
    {
        LoadVideoCommand = new RelayCommand(_ => LoadRequested?.Invoke());
        OpenPipCommand = new RelayCommand(_ => PipRequested?.Invoke());
        StopCommand = new RelayCommand(_ => StopRequested?.Invoke());
    }
}
