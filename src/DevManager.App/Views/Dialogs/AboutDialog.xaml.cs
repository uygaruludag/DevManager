using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace DevManager.App.Views.Dialogs;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
            TxtVersion.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private void EmailLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("mailto:info@istechlabs.com") { UseShellExecute = true });
        }
        catch { }
    }

    private void WebLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.istechlabs.com") { UseShellExecute = true });
        }
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
