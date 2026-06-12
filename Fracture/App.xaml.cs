using System.Windows;

namespace Fracture;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new Views.SplashScreen();
        splash.StartLoading();
        splash.ShowDialog();

        ShutdownMode = ShutdownMode.OnLastWindowClose;

        var main = new MainWindow();
        main.Show();
    }
}

