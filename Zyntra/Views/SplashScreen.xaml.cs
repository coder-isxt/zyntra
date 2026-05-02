using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Zyntra.Services;

namespace Zyntra.Views;

public partial class SplashScreen : Window
{
    private readonly DispatcherTimer _timer;
    private int _step;
    private double _trackWidth;
    private double _currentFraction;
    private readonly string[] _messages = [
        "Loading configuration...",
        "Preparing services...",
        "Loading accounts...",
        "Almost ready...",
    ];

    public SplashScreen()
    {
        InitializeComponent();
        VersionText.Text = $"v{UpdateService.CurrentVersion}";

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _timer.Tick += OnTick;
    }

    public void StartLoading()
    {
        _step = 0;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_step < _messages.Length)
        {
            StatusText.Text = _messages[_step];
            double progress = (double)(_step + 1) / _messages.Length;
            AnimateProgress(progress);
            _step++;
        }
        else
        {
            _timer.Stop();
            DialogResult = true;
            Close();
        }
    }

    private void AnimateProgress(double fraction)
    {
        _currentFraction = fraction;
        if (_trackWidth <= 0) return;
        double targetWidth = _trackWidth * fraction;
        var anim = new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        ProgressFill.BeginAnimation(WidthProperty, anim);
    }

    private void TrackBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _trackWidth = e.NewSize.Width;
        // Re-apply current fraction to match new width without animation
        if (_currentFraction > 0)
        {
            ProgressFill.BeginAnimation(WidthProperty, null);
            ProgressFill.Width = _trackWidth * _currentFraction;
        }
    }
}
