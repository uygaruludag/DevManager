using System.IO;
using System.Windows;
using System.Windows.Threading;
using DevManager.Core.Services;
using DevManager.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevManager.App;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var sc = new ServiceCollection();
        sc.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        sc.AddSingleton<IConfigurationService, ConfigurationService>();
        sc.AddSingleton<IProcessManagerService, ProcessManagerService>();
        sc.AddSingleton<ILogService>(new LogService(5000));
        sc.AddSingleton<IHealthCheckService, HealthCheckService>();

        Services = sc.BuildServiceProvider();

        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("Dispatcher", e.Exception);
        MessageBox.Show($"Hata:\n{e.Exception.Message}\n\n{e.Exception.InnerException?.Message}",
            "DevManager Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogError("AppDomain", ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogError("UnobservedTask", e.Exception);
        e.SetObserved(); // Uygulamanın kapanmasını engelle
    }

    internal static void LogError(string context, Exception ex)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevManager");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"),
                $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}\n");
        }
        catch { }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var pm = Services.GetRequiredService<IProcessManagerService>();
        await pm.StopAllAsync(force: true);
        Services.Dispose();
        base.OnExit(e);
    }
}
