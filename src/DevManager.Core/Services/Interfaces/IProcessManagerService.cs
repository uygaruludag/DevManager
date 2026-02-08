using System.Diagnostics;
using DevManager.Core.Models;

namespace DevManager.Core.Services.Interfaces;

public interface IProcessManagerService
{
    IReadOnlyDictionary<Guid, ProcessInstance> Instances { get; }

    Task StartProcessAsync(ProcessDefinition definition);
    Task StopProcessAsync(Guid definitionId, bool force = false);
    Task RestartProcessAsync(Guid definitionId);

    Task StartProjectAsync(Project project);
    Task StopProjectAsync(Project project, bool force = false);

    Task StopAllAsync(bool force = false);

    ProcessInstance? GetInstance(Guid definitionId);

    /// <summary>
    /// Yönetilen process'in System.Diagnostics.Process nesnesini döndürür.
    /// CPU/RAM gibi metrikler okumak için kullanılır.
    /// </summary>
    Process? GetSystemProcess(Guid definitionId);

    /// <summary>
    /// Önceki oturumdan kalan çalışan processleri tespit edip sahiplenir.
    /// </summary>
    Task<int> DetectAndAdoptOrphansAsync(IEnumerable<ProcessDefinition> definitions);

    event EventHandler<ProcessInstance>? ProcessStateChanged;
}
