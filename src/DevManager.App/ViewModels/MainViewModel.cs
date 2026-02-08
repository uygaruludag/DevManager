using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevManager.App.Localization;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;

namespace DevManager.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly IProcessManagerService _processManager;
    private readonly ILogService _logService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedProject))]
    private ProjectGroupViewModel? _selectedProject;

    [ObservableProperty]
    private bool _isLoading = true;

    public ObservableCollection<ProjectGroupViewModel> Projects { get; } = [];

    public bool HasSelectedProject => SelectedProject != null;

    public int TotalRunning => Projects.Sum(p => p.RunningCount);
    public int TotalProcesses => Projects.Sum(p => p.TotalCount);

    public MainViewModel(
        IConfigurationService configService,
        IProcessManagerService processManager,
        ILogService logService)
    {
        _configService = configService;
        _processManager = processManager;
        _logService = logService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var projects = await _configService.GetProjectsAsync();
            Projects.Clear();

            foreach (var project in projects.OrderBy(p => p.SortOrder))
            {
                var vm = new ProjectGroupViewModel(project, _processManager, _logService, _configService);
                vm.PropertyChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(TotalRunning));
                    OnPropertyChanged(nameof(TotalProcesses));
                };
                Projects.Add(vm);
            }

            if (Projects.Count > 0)
                SelectedProject = Projects[0];

            var allDefinitions = Projects.SelectMany(p => p.Processes.Select(vm => vm.Definition)).ToList();
            if (allDefinitions.Count > 0)
            {
                var adopted = await _processManager.DetectAndAdoptOrphansAsync(allDefinitions);
                if (adopted > 0)
                    _logService.AppendLog(Guid.Empty,
                        string.Format(Strings.Log_OrphanAdopted, adopted),
                        Core.Models.LogEntryType.System);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartAllAsync()
    {
        var tasks = Projects.Select(p => p.StartAllCommand.ExecuteAsync(null));
        await Task.WhenAll(tasks);
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        await _processManager.StopAllAsync();
    }

    [RelayCommand]
    private void AddProject()
    {
        var dialog = new Views.Dialogs.AddProjectDialog();
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            var project = dialog.Result;
            var vm = new ProjectGroupViewModel(project, _processManager, _logService, _configService);
            vm.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(TotalRunning));
                OnPropertyChanged(nameof(TotalProcesses));
            };
            Projects.Add(vm);
            SelectedProject = vm;
            _ = SaveConfigAsync();
        }
    }

    [RelayCommand]
    private void AddProcess()
    {
        if (SelectedProject == null) return;

        var dialog = new Views.Dialogs.AddProcessDialog();
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            var definition = dialog.Result;
            definition.ProjectId = SelectedProject.Project.Id;
            SelectedProject.AddProcess(definition);
            _ = SaveConfigAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteProjectAsync()
    {
        if (SelectedProject == null) return;

        var result = MessageBox.Show(
            string.Format(Strings.Project_DeleteConfirm, SelectedProject.Name),
            Strings.Project_DeleteTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        await SelectedProject.StopAllCommand.ExecuteAsync(null);
        SelectedProject.Cleanup();

        await _configService.DeleteProjectAsync(SelectedProject.Id);
        Projects.Remove(SelectedProject);
        SelectedProject = Projects.FirstOrDefault();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var dialog = new Views.Dialogs.AboutDialog();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void DeleteProcess(ProcessViewModel? processVm)
    {
        if (SelectedProject == null || processVm == null) return;

        var result = MessageBox.Show(
            string.Format(Strings.Process_DeleteConfirm, processVm.Name),
            Strings.Process_DeleteTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        SelectedProject.RemoveProcess(processVm);
        _ = SaveConfigAsync();
    }

    private async Task SaveConfigAsync()
    {
        var config = await _configService.LoadAsync();
        config.Projects = Projects.Select(p => p.Project).ToList();
        await _configService.SaveAsync(config);
    }
}
