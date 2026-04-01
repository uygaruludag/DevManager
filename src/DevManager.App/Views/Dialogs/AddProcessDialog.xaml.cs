using System.Windows;
using System.Windows.Controls;
using DevManager.App.Localization;
using DevManager.Core.Models;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace DevManager.App.Views.Dialogs;

public partial class AddProcessDialog : Window
{
    public ProcessDefinition? Result { get; private set; }
    private readonly ProcessDefinition? _editingProcess;
    private bool IsEditMode => _editingProcess != null;

    public AddProcessDialog(ProcessDefinition? existingProcess = null)
    {
        InitializeComponent();

        if (existingProcess != null)
        {
            _editingProcess = existingProcess;
            Title = Strings.Dialog_EditProcess_Title;
            TxtHeader.Text = Strings.Dialog_EditProcess_Header;
            BtnAdd.Content = Strings.Dialog_Save;

            // Mevcut değerleri doldur
            TxtName.Text = existingProcess.Name;
            TxtCommand.Text = existingProcess.Command;
            TxtArguments.Text = existingProcess.Arguments;
            TxtWorkingDir.Text = existingProcess.WorkingDirectory;
            ChkAutoRestart.IsChecked = existingProcess.AutoRestartOnCrash;
            TxtMaxRetries.Text = existingProcess.MaxRestartAttempts.ToString();
            TxtRestartDelay.Text = existingProcess.RestartDelaySeconds.ToString();
            TxtStartupDelay.Text = existingProcess.StartupDelaySeconds.ToString();
            ChkAutoStartWithProject.IsChecked = existingProcess.AutoStartWithProject;

            // Environment variables
            if (existingProcess.EnvironmentVariables.Count > 0)
            {
                TxtEnvVars.Text = string.Join("\n",
                    existingProcess.EnvironmentVariables.Select(kv => $"{kv.Key}={kv.Value}"));
            }

            // Notification mode
            CmbNotificationMode.SelectedIndex = existingProcess.NotificationMode switch
            {
                NotificationMode.Off => 0,
                NotificationMode.CrashOnly => 1,
                NotificationMode.ErrorOnly => 2,
                NotificationMode.ErrorAndWarning => 3,
                _ => 1
            };
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show(Strings.Dialog_AddProcess_NameRequired, Strings.Warning_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtCommand.Text))
        {
            MessageBox.Show(Strings.Dialog_AddProcess_CommandRequired, Strings.Warning_Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ProcessDefinition definition;

        if (IsEditMode)
        {
            // Edit modu: mevcut definition'ı güncelle
            definition = _editingProcess!;
            definition.Name = TxtName.Text.Trim();
            definition.Command = TxtCommand.Text.Trim();
            definition.Arguments = TxtArguments.Text.Trim();
            definition.WorkingDirectory = TxtWorkingDir.Text.Trim();
            definition.AutoRestartOnCrash = ChkAutoRestart.IsChecked == true;
            definition.MaxRestartAttempts = int.TryParse(TxtMaxRetries.Text, out var mr2) ? mr2 : 3;
            definition.RestartDelaySeconds = int.TryParse(TxtRestartDelay.Text, out var rd2) ? rd2 : 5;
            definition.StartupDelaySeconds = int.TryParse(TxtStartupDelay.Text, out var sd2) ? sd2 : 0;
            definition.AutoStartWithProject = ChkAutoStartWithProject.IsChecked == true;
            definition.NotificationMode = ParseNotificationMode();
            definition.EnvironmentVariables.Clear();
        }
        else
        {
            definition = new ProcessDefinition
            {
                Name = TxtName.Text.Trim(),
                Command = TxtCommand.Text.Trim(),
                Arguments = TxtArguments.Text.Trim(),
                WorkingDirectory = TxtWorkingDir.Text.Trim(),
                AutoRestartOnCrash = ChkAutoRestart.IsChecked == true,
                MaxRestartAttempts = int.TryParse(TxtMaxRetries.Text, out var mr) ? mr : 3,
                RestartDelaySeconds = int.TryParse(TxtRestartDelay.Text, out var rd) ? rd : 5,
                StartupDelaySeconds = int.TryParse(TxtStartupDelay.Text, out var sd) ? sd : 0,
                AutoStartWithProject = ChkAutoStartWithProject.IsChecked == true,
                NotificationMode = ParseNotificationMode()
            };
        }

        // Parse environment variables
        var envText = TxtEnvVars.Text.Trim();
        if (!string.IsNullOrEmpty(envText))
        {
            foreach (var line in envText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('=', 2);
                if (parts.Length == 2)
                    definition.EnvironmentVariables[parts[0].Trim()] = parts[1].Trim();
            }
        }

        Result = definition;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BrowseCommand_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = Strings.Dialog_AddProcess_FileFilter,
            Title = Strings.Dialog_AddProcess_BrowseCommand
        };

        if (dialog.ShowDialog() == true)
            TxtCommand.Text = dialog.FileName;
    }

    private NotificationMode ParseNotificationMode()
    {
        var tag = (CmbNotificationMode.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return tag switch
        {
            "Off" => NotificationMode.Off,
            "CrashOnly" => NotificationMode.CrashOnly,
            "ErrorOnly" => NotificationMode.ErrorOnly,
            "ErrorAndWarning" => NotificationMode.ErrorAndWarning,
            _ => NotificationMode.CrashOnly
        };
    }

    private void BrowseWorkingDir_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinForms.FolderBrowserDialog
        {
            Description = Strings.Dialog_AddProcess_BrowseWorkDir,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            TxtWorkingDir.Text = dialog.SelectedPath;
    }
}
