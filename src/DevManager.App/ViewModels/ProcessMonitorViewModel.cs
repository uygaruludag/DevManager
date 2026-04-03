using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevManager.App.Localization;
using DevManager.Core.Models;
using DevManager.Core.Services;
using DevManager.Core.Services.Interfaces;

namespace DevManager.App.ViewModels;

public partial class ProcessMonitorViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public ObservableCollection<SystemProcessInfo> AllProcesses { get; } = [];
    public ObservableCollection<SystemProcessInfo> FilteredProcesses { get; } = [];

    public ProcessMonitorViewModel(IConfigurationService configService)
    {
        _configService = configService;
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;
        IsScanning = true;
        StatusText = Strings.Monitor_Scanning;

        try
        {
            var config = await _configService.LoadAsync();
            var processes = await Task.Run(() => SystemProcessScanner.Scan(config.Projects));

            AllProcesses.Clear();
            foreach (var p in processes)
                AllProcesses.Add(p);

            ApplyFilter();

            StatusText = string.Format(Strings.Monitor_Found, AllProcesses.Count);
        }
        catch (Exception ex)
        {
            StatusText = $"Hata: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void KillProcess(SystemProcessInfo? process)
    {
        if (process == null) return;

        var result = MessageBox.Show(
            string.Format(Strings.Monitor_KillConfirm, process.Name, process.Pid),
            Strings.Monitor_KillTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        if (SystemProcessScanner.KillProcess(process.Pid))
        {
            AllProcesses.Remove(process);
            FilteredProcesses.Remove(process);
            StatusText = string.Format(Strings.Monitor_Killed, process.Pid);
        }
        else
        {
            MessageBox.Show(
                string.Format(Strings.Monitor_KillFailed, process.Pid),
                Strings.Error_Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        FilteredProcesses.Clear();
        var filter = FilterText?.Trim().ToLowerInvariant() ?? "";

        foreach (var p in AllProcesses)
        {
            if (string.IsNullOrEmpty(filter) ||
                p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                p.CommandLine.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                p.Pid.ToString().Contains(filter) ||
                p.PortsDisplay.Contains(filter) ||
                (p.MatchedProjectName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                FilteredProcesses.Add(p);
            }
        }
    }
}
