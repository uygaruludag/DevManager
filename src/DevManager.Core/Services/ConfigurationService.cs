using System.Text.Json;
using System.Text.Json.Serialization;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;

namespace DevManager.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DevManager");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "devmanager-config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private DevManagerConfig? _cachedConfig;

    public async Task<DevManagerConfig> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            if (!File.Exists(ConfigPath))
            {
                _cachedConfig = new DevManagerConfig();
                await SaveInternalAsync(_cachedConfig);
                return _cachedConfig;
            }

            var json = await File.ReadAllTextAsync(ConfigPath);
            _cachedConfig = JsonSerializer.Deserialize<DevManagerConfig>(json, JsonOptions)
                           ?? new DevManagerConfig();
            return _cachedConfig;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(DevManagerConfig config)
    {
        await _lock.WaitAsync();
        try
        {
            _cachedConfig = config;
            await SaveInternalAsync(config);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        var config = await LoadAsync();
        return config.Projects;
    }

    public async Task SaveProjectAsync(Project project)
    {
        var config = await LoadAsync();
        var existing = config.Projects.FindIndex(p => p.Id == project.Id);
        if (existing >= 0)
            config.Projects[existing] = project;
        else
            config.Projects.Add(project);

        await SaveAsync(config);
    }

    public async Task DeleteProjectAsync(Guid projectId)
    {
        var config = await LoadAsync();
        config.Projects.RemoveAll(p => p.Id == projectId);
        await SaveAsync(config);
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        var config = await LoadAsync();
        return config.Settings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var config = await LoadAsync();
        config.Settings = settings;
        await SaveAsync(config);
    }

    private async Task SaveInternalAsync(DevManagerConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var tempPath = ConfigPath + ".tmp";
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(tempPath, json);
        File.Move(tempPath, ConfigPath, overwrite: true);
    }
}
