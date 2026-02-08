namespace DevManager.Core.Events;

public record ProcessCrashedEvent(
    Guid ProcessDefinitionId,
    int ExitCode,
    string? ErrorMessage,
    bool WillAutoRestart
);
