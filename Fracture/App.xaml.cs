using System.Windows;

namespace Fracture;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, args) => args.SetObserved();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new Views.SplashScreen();
        splash.StartLoading();
        splash.ShowDialog();

        ShutdownMode = ShutdownMode.OnLastWindowClose;

        var main = new MainWindow();
        main.Show();
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}",
            "Fracture", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}

