using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;

namespace DevManager.App.ViewModels;

public partial class ProjectGroupViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly IProcessManagerService _processManager;
    private readonly ILogService _logService;
    private readonly IConfigurationService _configService;

    public Project Project => _project;
    public Guid Id => _project.Id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _color = "#2196F3";

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<ProcessViewModel> Processes { get; } = [];

    public int RunningCount => Processes.Count(p => p.State == ProcessState.Running);
    public int TotalCount => Processes.Count;
    public string StatusText => $"{RunningCount}/{TotalCount}";

    public ProjectGroupViewModel(
        Project project,
        IProcessManagerService processManager,
        ILogService logService,
        IConfigurationService configService)
    {
        _project = project;
        _processManager = processManager;
        _logService = logService;
        _configService = configService;

        _name = project.Name;
        _description = project.Description;
        _color = project.Color;

        foreach (var procDef in project.Processes.OrderBy(p => p.SortOrder))
        {
            var vm = new ProcessViewModel(procDef, processManager, logService);
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ProcessViewModel.State))
                {
                    OnPropertyChanged(nameof(RunningCount));
                    OnPropertyChanged(nameof(StatusText));
                }
            };
            Processes.Add(vm);
        }
    }

    [RelayCommand]
    private async Task StartAllAsync()
    {
        await _processManager.StartProjectAsync(_project);
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        await _processManager.StopProjectAsync(_project);
    }

    [RelayCommand]
    private async Task RestartAllAsync()
    {
        await _processManager.StopProjectAsync(_project);
        await Task.Delay(500);
        await _processManager.StartProjectAsync(_project);
    }

    public void AddProcess(ProcessDefinition definition)
    {
        _project.Processes.Add(definition);
        var vm = new ProcessViewModel(definition, _processManager, _logService);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProcessViewModel.State))
            {
                OnPropertyChanged(nameof(RunningCount));
                OnPropertyChanged(nameof(StatusText));
            }
        };
        Processes.Add(vm);
    }

    public void RemoveProcess(ProcessViewModel processVm)
    {
        processVm.Cleanup();
        _project.Processes.RemoveAll(p => p.Id == processVm.Id);
        Processes.Remove(processVm);
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(StatusText));
    }

    public void Cleanup()
    {
        foreach (var p in Processes)
            p.Cleanup();
    }
}
