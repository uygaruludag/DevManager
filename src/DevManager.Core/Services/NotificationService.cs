using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;

namespace DevManager.Core.Services;

public class NotificationService : INotificationService
{
    private readonly ILogService _logService;
    private readonly IProcessManagerService _processManager;
    private readonly IConfigurationService _configService;

    private Func<Guid, (string? ProjectName, string? ProcessName)> _nameResolver = _ => (null, null);

    /// <summary>
    /// Progressive throttle: Process başına artan bekleme süresi.
    /// İlk hata → anında bildirim
    /// 2. hata → 10sn bekle
    /// 3. hata → 30sn bekle
    /// 4+ hata → 60sn bekle
    /// Hata kesilirse (60sn sessizlik) → sıfırlanır
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ThrottleState> _throttleStates = new();

    // Regex pattern'leri (case-insensitive, compiled)
    private static readonly Regex ErrorPattern = new(
        @"\b(exception|error|fail(ed|ure)?|fatal|unhandled|crash(ed)?|critical|stack\s*trace|null\s*reference)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex WarningPattern = new(
        @"\b(warn(ing)?|deprecated|timeout|timed?\s*out|retry|retrying)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // False positive'leri filtrele
    private static readonly Regex FalsePositivePattern = new(
        @"(error\s*count\s*:\s*0|0\s+error|errors?\s*:\s*0|no\s+errors?|warn(ing)?s?\s*:\s*0|0\s+warn|successfully|succeeded)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public event EventHandler<ProcessNotification>? NotificationReceived;

    public NotificationService(
        ILogService logService,
        IProcessManagerService processManager,
        IConfigurationService configService)
    {
        _logService = logService;
        _processManager = processManager;
        _configService = configService;
    }

    public void Initialize(Func<Guid, (string? ProjectName, string? ProcessName)> nameResolver)
    {
        _nameResolver = nameResolver;
        _logService.LogReceived += OnLogReceived;
        _processManager.ProcessStateChanged += OnProcessStateChanged;
    }

    private async void OnLogReceived(object? sender, LogEntry entry)
    {
        // System mesajlarını atla
        if (entry.Type == LogEntryType.System)
            return;

        // Process bildirim modunu kontrol et
        var mode = await GetNotificationModeAsync(entry.ProcessDefinitionId);
        if (mode == NotificationMode.Off)
            return;

        var text = entry.Text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        // False positive kontrolü
        if (FalsePositivePattern.IsMatch(text))
            return;

        NotificationLevel? level = null;

        if (ErrorPattern.IsMatch(text))
            level = NotificationLevel.Error;
        else if (WarningPattern.IsMatch(text))
            level = NotificationLevel.Warning;

        if (level == null)
            return;

        // ErrorOnly modundaysa warning'leri atla
        if (mode == NotificationMode.ErrorOnly && level == NotificationLevel.Warning)
            return;

        // Progressive throttle kontrolü
        if (!ShouldNotify(entry.ProcessDefinitionId))
            return;

        var (projectName, processName) = _nameResolver(entry.ProcessDefinitionId);

        // Mesajı kırp (max 150 karakter)
        var message = text.Length > 150 ? text[..150] + "..." : text;

        var notification = new ProcessNotification(
            entry.ProcessDefinitionId,
            projectName ?? "Unknown",
            processName ?? "Unknown",
            message,
            level.Value,
            DateTime.Now
        );

        NotificationReceived?.Invoke(this, notification);
    }

    private async void OnProcessStateChanged(object? sender, ProcessInstance instance)
    {
        if (instance.State != ProcessState.Crashed)
            return;

        // Off modundaysa crash bildirimi de gönderme
        var mode = await GetNotificationModeAsync(instance.DefinitionId);
        if (mode == NotificationMode.Off)
            return;

        var (projectName, processName) = _nameResolver(instance.DefinitionId);
        var exitCode = instance.LastExitCode?.ToString() ?? "?";

        var notification = new ProcessNotification(
            instance.DefinitionId,
            projectName ?? "Unknown",
            processName ?? "Unknown",
            $"Process crashed (exit code: {exitCode})",
            NotificationLevel.Critical,
            DateTime.Now
        );

        NotificationReceived?.Invoke(this, notification);
    }

    /// <summary>
    /// Progressive throttle: Sürekli hata akışında bildirim sıklığını azaltır.
    /// </summary>
    private bool ShouldNotify(Guid processId)
    {
        var now = DateTime.Now;
        var state = _throttleStates.GetOrAdd(processId, _ => new ThrottleState());

        lock (state)
        {
            // 60sn sessizlik olduysa → sıfırla, ilk hata gibi davran
            if (state.LastErrorTime.HasValue && (now - state.LastErrorTime.Value).TotalSeconds > 60)
            {
                state.ConsecutiveErrors = 0;
                state.LastNotificationTime = null;
            }

            state.LastErrorTime = now;
            state.ConsecutiveErrors++;

            // İlk hata → anında bildirim
            if (state.ConsecutiveErrors == 1)
            {
                state.LastNotificationTime = now;
                return true;
            }

            // Bekleme süresi: 2.hata=10sn, 3.hata=30sn, 4+=60sn
            var cooldownSeconds = state.ConsecutiveErrors switch
            {
                2 => 10,
                3 => 30,
                _ => 60
            };

            if (state.LastNotificationTime.HasValue &&
                (now - state.LastNotificationTime.Value).TotalSeconds < cooldownSeconds)
            {
                return false;
            }

            state.LastNotificationTime = now;
            return true;
        }
    }

    private async Task<NotificationMode> GetNotificationModeAsync(Guid processDefinitionId)
    {
        try
        {
            var config = await _configService.LoadAsync();
            var process = config.Projects
                .SelectMany(p => p.Processes)
                .FirstOrDefault(p => p.Id == processDefinitionId);

            return process?.NotificationMode ?? NotificationMode.ErrorOnly;
        }
        catch
        {
            return NotificationMode.ErrorOnly;
        }
    }

    public void Dispose()
    {
        _logService.LogReceived -= OnLogReceived;
        _processManager.ProcessStateChanged -= OnProcessStateChanged;
    }

    private class ThrottleState
    {
        public int ConsecutiveErrors;
        public DateTime? LastErrorTime;
        public DateTime? LastNotificationTime;
    }
}
