using DevManager.Core.Models;

namespace DevManager.Core.Events;

public record LogReceivedEvent(LogEntry Entry);
