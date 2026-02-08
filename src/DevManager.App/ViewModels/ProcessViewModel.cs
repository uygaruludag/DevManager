using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;

namespace DevManager.App.ViewModels;

public partial class ProcessViewModel : ObservableObject
{
    private readonly ProcessDefinition _definition;
    private readonly IProcessManagerService _processManager;
    private readonly ILogService _logService;
    private readonly DispatcherTimer _uptimeTimer;
    private readonly DispatcherTimer _logFlushTimer;
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();

    public ProcessDefinition Definition => _definition;
    public Guid Id => _definition.Id;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    private ProcessState _state = ProcessState.Stopped;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int? _processId;

    [ObservableProperty]
    private string _uptime = string.Empty;

    [ObservableProperty]
    private int _restartCount;

    [ObservableProperty]
    private string _logFilter = string.Empty;

    // Resource metrics
    [ObservableProperty]
    private string _cpuUsage = string.Empty;

    [ObservableProperty]
    private string _memoryUsage = string.Empty;

    // CPU hesaplama için önceki değerler
    private TimeSpan _lastTotalProcessorTime;
    private DateTime _lastCpuCheckTime;
    private bool _hasPreviousCpuReading;

    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    private DateTime? _startedAt;

    public ProcessViewModel(ProcessDefinition definition, IProcessManagerService processManager, ILogService logService)
    {
        _definition = definition;
        _processManager = processManager;
        _logService = logService;
        _name = definition.Name;

        _logService.LogReceived += OnLogReceived;
        _processManager.ProcessStateChanged += OnProcessStateChanged;

        _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uptimeTimer.Tick += (_, _) =>
        {
            UpdateUptime();
            UpdateMetrics();
        };

        // Batch log updates every 100ms instead of per-line
        _logFlushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _logFlushTimer.Tick += (_, _) => FlushLogQueue();
        _logFlushTimer.Start();

        // Load existing state
        var instance = _processManager.GetInstance(definition.Id);
        if (instance != null)
            ApplyState(instance);
    }

    private bool CanStart => State is ProcessState.Stopped or ProcessState.Crashed;
    private bool CanStop => State is ProcessState.Running or ProcessState.Starting;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        await _processManager.StartProcessAsync(_definition);
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        await _processManager.StopProcessAsync(_definition.Id);
    }

    [RelayCommand]
    private async Task RestartAsync()
    {
        await _processManager.RestartProcessAsync(_definition.Id);
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
        _logService.ClearLogs(_definition.Id);
    }

    [RelayCommand]
    private void CopyLogs()
    {
        var text = string.Join(Environment.NewLine,
            LogEntries.Select(e => $"[{e.Timestamp:HH:mm:ss}] {e.Text}"));
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    private void OnLogReceived(object? sender, LogEntry entry)
    {
        if (entry.ProcessDefinitionId != _definition.Id) return;
        _logQueue.Enqueue(entry);
    }

    private void FlushLogQueue()
    {
        const int maxPerFlush = 200;
        var count = 0;

        while (count < maxPerFlush && _logQueue.TryDequeue(out var entry))
        {
            LogEntries.Add(entry);
            count++;
        }

        // Trim excess
        while (LogEntries.Count > 5000)
            LogEntries.RemoveAt(0);
    }

    private void OnProcessStateChanged(object? sender, ProcessInstance instance)
    {
        if (instance.DefinitionId != _definition.Id) return;

        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            ApplyState(instance);
        });
    }

    private void ApplyState(ProcessInstance instance)
    {
        State = instance.State;
        ProcessId = instance.ProcessId;
        RestartCount = instance.RestartCount;

        if (instance.State == ProcessState.Running && instance.StartedAt.HasValue)
        {
            _startedAt = instance.StartedAt;
            _hasPreviousCpuReading = false;
            _uptimeTimer.Start();
            UpdateUptime();
        }
        else
        {
            _startedAt = null;
            _uptimeTimer.Stop();
            Uptime = string.Empty;
            CpuUsage = string.Empty;
            MemoryUsage = string.Empty;
            _hasPreviousCpuReading = false;
        }
    }

    private void UpdateUptime()
    {
        if (_startedAt == null) return;
        var elapsed = DateTime.Now - _startedAt.Value;
        Uptime = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}h{elapsed.Minutes:D2}m"
            : $"{elapsed.Minutes}m{elapsed.Seconds:D2}s";
    }

    private void UpdateMetrics()
    {
        if (State != ProcessState.Running || ProcessId == null)
            return;

        try
        {
            var process = _processManager.GetSystemProcess(_definition.Id);
            if (process == null || process.HasExited)
            {
                CpuUsage = string.Empty;
                MemoryUsage = string.Empty;
                return;
            }

            // RAM: WorkingSet64 → MB
            var memoryMb = process.WorkingSet64 / 1024.0 / 1024.0;
            MemoryUsage = memoryMb >= 1024
                ? $"{memoryMb / 1024:F1} GB"
                : $"{memoryMb:F0} MB";

            // CPU: Delta hesaplaması
            var currentCpuTime = process.TotalProcessorTime;
            var now = DateTime.UtcNow;

            if (_hasPreviousCpuReading)
            {
                var cpuDelta = (currentCpuTime - _lastTotalProcessorTime).TotalMilliseconds;
                var timeDelta = (now - _lastCpuCheckTime).TotalMilliseconds;

                if (timeDelta > 0)
                {
                    var cpuPercent = cpuDelta / timeDelta / Environment.ProcessorCount * 100.0;
                    cpuPercent = Math.Min(cpuPercent, 100.0); // Clamp
                    CpuUsage = $"{cpuPercent:F1}%";
                }
            }

            _lastTotalProcessorTime = currentCpuTime;
            _lastCpuCheckTime = now;
            _hasPreviousCpuReading = true;
        }
        catch
        {
            // Process erişilemez olabilir (kapanmış, yetki sorunu vb.)
            CpuUsage = string.Empty;
            MemoryUsage = string.Empty;
            _hasPreviousCpuReading = false;
        }
    }

    public void Cleanup()
    {
        _logService.LogReceived -= OnLogReceived;
        _processManager.ProcessStateChanged -= OnProcessStateChanged;
        _uptimeTimer.Stop();
        _logFlushTimer.Stop();
    }
}
