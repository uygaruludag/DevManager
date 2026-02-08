namespace DevManager.Core.Models;

public class ProcessInstance
{
    public Guid DefinitionId { get; set; }
    public ProcessState State { get; set; } = ProcessState.Stopped;
    public int? ProcessId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public int RestartCount { get; set; }
    public DateTime? LastRestartWindowStart { get; set; }
    public int? LastExitCode { get; set; }
    public string? LastError { get; set; }
}
