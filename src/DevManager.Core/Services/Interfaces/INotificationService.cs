using DevManager.Core.Models;

namespace DevManager.Core.Services.Interfaces;

public interface INotificationService : IDisposable
{
    event EventHandler<ProcessNotification>? NotificationReceived;
    void Initialize(Func<Guid, (string? ProjectName, string? ProcessName)> nameResolver);
}

public record ProcessNotification(
    Guid ProcessDefinitionId,
    string ProjectName,
    string ProcessName,
    string Message,
    NotificationLevel Level,
    DateTime Timestamp
);

public enum NotificationLevel
{
    Warning,
    Error,
    Critical
}
