namespace DevManager.Core.Models;

public class ProcessDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
    public int SortOrder { get; set; }

    // Auto-restart
    public bool AutoRestartOnCrash { get; set; } = true;
    public int MaxRestartAttempts { get; set; } = 3;
    public int RestartDelaySeconds { get; set; } = 5;
    public int RestartWindowMinutes { get; set; } = 10;

    // Health check
    public HealthCheckConfig? HealthCheck { get; set; }

    // Startup
    public int StartupDelaySeconds { get; set; }
    public bool AutoStartWithProject { get; set; } = true;

    // Notifications — Off, ErrorOnly (default), ErrorAndWarning
    public NotificationMode NotificationMode { get; set; } = NotificationMode.ErrorOnly;
}

public enum NotificationMode
{
    Off,
    ErrorOnly,
    ErrorAndWarning
}
