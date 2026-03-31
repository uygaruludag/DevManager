namespace DevManager.Core.Models;

/// <summary>
/// Bir process başlatılmaya çalışıldığında port çakışması tespit edildiğinde kullanılır.
/// </summary>
public class PortConflictEventArgs : EventArgs
{
    public Guid ProcessDefinitionId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public int Port { get; init; }
    public int ConflictingPid { get; init; }
    public string? ConflictingProcessName { get; init; }

    /// <summary>
    /// UI tarafından set edilir. true = çakışan process'i öldür ve devam et.
    /// </summary>
    public bool KillRequested { get; set; }
}
