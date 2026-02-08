using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DevManager.Core.Models;
using DevManager.Core.Services;
using WinForms = System.Windows.Forms;

namespace DevManager.App.Views.Dialogs;

public partial class AddProjectDialog : Window
{
    public Project? Result { get; private set; }

    public ObservableCollection<ScannedProcess> ScannedProcesses { get; } = [];

    public AddProjectDialog()
    {
        InitializeComponent();
    }

    private void Scan_Click(object sender, RoutedEventArgs e)
    {
        var path = TxtPath.Text.Trim();
        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show("Lütfen bir klasör yolu girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!System.IO.Directory.Exists(path))
        {
            MessageBox.Show("Belirtilen klasör bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ScannedProcesses.Clear();

        var definitions = ProjectScanner.ScanDirectory(path);

        if (definitions.Count == 0)
        {
            TxtScanStatus.Text = "Hiçbir çalıştırılabilir proje bulunamadı.";
            return;
        }

        foreach (var def in definitions)
        {
            ScannedProcesses.Add(new ScannedProcess { Definition = def, IsSelected = true });
        }

        LstProcesses.ItemsSource = ScannedProcesses;
        TxtScanStatus.Text = $"{definitions.Count} süreç tespit edildi:";

        // Auto-fill project name from folder name if empty
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            TxtName.Text = System.IO.Path.GetFileName(path);
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Proje adı gereklidir.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedColor = (CmbColor.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#2196F3";

        var project = new Project
        {
            Name = TxtName.Text.Trim(),
            Color = selectedColor,
            AutoStartOnLaunch = ChkAutoStart.IsChecked == true
        };

        // Add selected scanned processes
        var selectedProcesses = ScannedProcesses.Where(sp => sp.IsSelected).ToList();
        foreach (var sp in selectedProcesses)
        {
            sp.Definition.ProjectId = project.Id;
            project.Processes.Add(sp.Definition);
        }

        Result = project;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BrowsePath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "Proje Klasörü Seçin",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            TxtPath.Text = dialog.SelectedPath;
        }
    }
}

public class ScannedProcess : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public ProcessDefinition Definition { get; set; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
