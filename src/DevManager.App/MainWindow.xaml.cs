using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using DevManager.App.Localization;
using DevManager.App.ViewModels;
using DevManager.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DevManager.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var configService = App.Services.GetRequiredService<IConfigurationService>();
        var processManager = App.Services.GetRequiredService<IProcessManagerService>();
        var logService = App.Services.GetRequiredService<ILogService>();

        var vm = new MainViewModel(configService, processManager, logService);
        DataContext = vm;

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
