using DevManager.Core.Models;

namespace DevManager.Core.Services.Interfaces;

public interface ILogService
{
    void AppendLog(Guid processDefinitionId, string text, LogEntryType type);
    IReadOnlyList<LogEntry> GetLogs(Guid processDefinitionId);
    void ClearLogs(Guid processDefinitionId);
    event EventHandler<LogEntry>? LogReceived;
}
