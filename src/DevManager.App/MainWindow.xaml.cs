using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DevManager.App.Localization;
using DevManager.App.ViewModels;
using DevManager.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using WinForms = System.Windows.Forms;

namespace DevManager.App;

public partial class MainWindow : Window
{
    private INotificationService? _notificationService;
    private WinForms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/app.ico", UriKind.Absolute));
        }
        catch { }

        InitializeNotifyIcon();

        var configService = App.Services.GetRequiredService<IConfigurationService>();
        var processManager = App.Services.GetRequiredService<IProcessManagerService>();
        var logService = App.Services.GetRequiredService<ILogService>();

        var vm = new MainViewModel(configService, processManager, logService);
        DataContext = vm;

        InitializeNotifications(vm);

        Loaded += async (_, _) =>
        {
            try
            {
                await vm.LoadCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Strings.Error_LoadFailed, ex.Message), Strings.Error_Title);
            }
        };

        ContentRendered += (_, _) =>
        {
            Activate();
        };

        Closed += (_, _) =>
        {
            _notifyIcon?.Dispose();
        };
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new WinForms.NotifyIcon
        {
            Text = "DevManager",
            Visible = true
        };

        // app.ico'yu stream olarak yükle
        try
        {
            var iconUri = new Uri("pack://application:,,,/app.ico", UriKind.Absolute);
            var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
            if (streamInfo != null)
            {
                _notifyIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
            }
        }
        catch
        {
            // Fallback: varsayılan icon
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        // Tray icon'a tıklayınca pencereyi göster
        _notifyIcon.DoubleClick += (_, _) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };
    }

    private void InitializeNotifications(MainViewModel vm)
    {
        _notificationService = App.Services.GetRequiredService<INotificationService>();

        // Proje ve process adını çözen fonksiyon
        _notificationService.Initialize(processId =>
        {
            foreach (var project in vm.Projects)
            {
                var proc = project.Processes.FirstOrDefault(p => p.Id == processId);
                if (proc != null)
                    return (project.Name, proc.Name);
            }
            return (null, null);
        });

        _notificationService.NotificationReceived += OnNotificationReceived;
    }

    private void OnNotificationReceived(object? sender, ProcessNotification notification)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_notifyIcon == null) return;

            var tipIcon = notification.Level switch
            {
                NotificationLevel.Critical => WinForms.ToolTipIcon.Error,
                NotificationLevel.Error => WinForms.ToolTipIcon.Error,
                NotificationLevel.Warning => WinForms.ToolTipIcon.Warning,
                _ => WinForms.ToolTipIcon.Info
            };

            var title = $"{notification.ProjectName} — {notification.ProcessName}";

            // Max 63 karakter (Windows balloon sınırı: 64 - null)
            var message = notification.Message.Length > 63
                ? notification.Message[..60] + "..."
                : notification.Message;

            _notifyIcon.ShowBalloonTip(3000, title, message, tipIcon);
        });
    }

    private bool _languageInitialized;

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // İlk yüklemede tetiklenmesin
        if (!_languageInitialized)
        {
            _languageInitialized = true;
            return;
        }

        if (DataContext is MainViewModel vm && sender is ComboBox cmb)
        {
            vm.ChangeLanguageCommand.Execute(cmb.SelectedIndex);
        }
    }

    private void LogList_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.ItemsSource is not INotifyCollectionChanged collection) return;

        collection.CollectionChanged += (_, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Add && listBox.Items.Count > 0)
            {
                listBox.ScrollIntoView(listBox.Items[^1]);
            }
        };
    }
}
