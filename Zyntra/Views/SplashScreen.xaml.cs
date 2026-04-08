using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Zyntra.Views;

public partial class SplashScreen : Window
{
    private readonly DispatcherTimer _timer;
    private int _step;
    private readonly string[] _messages = [
        "Loading configuration...",
        "Preparing services...",
        "Loading accounts...",
        "Almost ready...",
    ];

    public SplashScreen()
    {
        InitializeComponent();

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
        double targetWidth = (ActualWidth - 60) * fraction;
        var anim = new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        ProgressFill.BeginAnimation(WidthProperty, anim);
    }
}
