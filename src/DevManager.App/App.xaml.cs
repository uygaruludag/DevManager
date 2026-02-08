using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using DevManager.App.Localization;
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
        // Dil ayarını yükle (config'den önce basit dosya okuma)
        InitializeLanguage();

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

    private static void InitializeLanguage()
    {
        try
        {
            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DevManager", "devmanager-config.json");

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                // Basit JSON parse - sadece "language" değerini bul
                var langMatch = System.Text.RegularExpressions.Regex.Match(
                    json, "\"language\"\\s*:\\s*\"(\\w{2})\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (langMatch.Success)
                {
                    var lang = langMatch.Groups[1].Value;
                    var culture = new CultureInfo(lang);
                    Thread.CurrentThread.CurrentUICulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    return;
                }
            }
        }
        catch { }

        // Varsayılan: Türkçe
        var trCulture = new CultureInfo("tr");
        Thread.CurrentThread.CurrentUICulture = trCulture;
        CultureInfo.DefaultThreadCurrentUICulture = trCulture;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("Dispatcher", e.Exception);
        MessageBox.Show(
            string.Format(Strings.Error_AppError, e.Exception.Message, e.Exception.InnerException?.Message),
            Strings.Error_AppErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
        e.SetObserved();
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
