using DevManager.Core.Models;

namespace DevManager.Core.Services.Interfaces;

public interface IHealthCheckService : IDisposable
{
    void Register(ProcessDefinition definition);
    void Unregister(Guid definitionId);
    Task HandleCrashedAsync(Guid definitionId);
    event EventHandler<Guid>? RestartRequested;
}
