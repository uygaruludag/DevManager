using DevManager.Core.Models;

namespace DevManager.Core.Services.Interfaces;

public interface IConfigurationService
{
    Task<DevManagerConfig> LoadAsync();
    Task SaveAsync(DevManagerConfig config);
    Task<List<Project>> GetProjectsAsync();
    Task SaveProjectAsync(Project project);
    Task DeleteProjectAsync(Guid projectId);
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
