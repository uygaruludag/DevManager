namespace DevManager.Core.Models;

public record LogEntry(
    Guid ProcessDefinitionId,
    DateTime Timestamp,
    string Text,
    LogEntryType Type
);

public enum LogEntryType
{
    StdOut,
    StdErr,
    System
}
