using DevManager.Core.Models;

namespace DevManager.Core.Events;

public record ProcessStateChangedEvent(
    Guid ProcessDefinitionId,
    ProcessState OldState,
    ProcessState NewState,
    int? ProcessId
);
