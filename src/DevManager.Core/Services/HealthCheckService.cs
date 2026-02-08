using System.Collections.Concurrent;
using System.Net.Sockets;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevManager.Core.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ConcurrentDictionary<Guid, HealthCheckState> _checks = new();
    private readonly HttpClient _httpClient;

    public event EventHandler<Guid>? RestartRequested;

    public HealthCheckService(ILogger<HealthCheckService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public void Register(ProcessDefinition definition)
    {
        if (definition.HealthCheck == null || definition.HealthCheck.Type == HealthCheckType.ProcessRunning)
            return;

        var state = new HealthCheckState
        {
            Definition = definition,
            ConsecutiveFailures = 0
        };

        state.Timer = new Timer(
            _ => _ = CheckHealthAsync(definition.Id),
            null,
            TimeSpan.FromSeconds(definition.HealthCheck.IntervalSeconds),
            TimeSpan.FromSeconds(definition.HealthCheck.IntervalSeconds));

        _checks[definition.Id] = state;
    }

    public void Unregister(Guid definitionId)
    {
        if (_checks.TryRemove(definitionId, out var state))
        {
            state.Timer?.Dispose();
        }
    }

    public async Task HandleCrashedAsync(Guid definitionId)
    {
        RestartRequested?.Invoke(this, definitionId);
    }

    private async Task CheckHealthAsync(Guid definitionId)
    {
        if (!_checks.TryGetValue(definitionId, out var state))
            return;

        var config = state.Definition.HealthCheck!;
        var healthy = false;

        try
        {
            healthy = config.Type switch
            {
                HealthCheckType.HttpEndpoint => await CheckHttpAsync(config),
                HealthCheckType.TcpPort => await CheckTcpAsync(config),
                _ => true
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed for {Id}", definitionId);
        }

        if (healthy)
        {
            state.ConsecutiveFailures = 0;
        }
        else
        {
            state.ConsecutiveFailures++;
            if (state.ConsecutiveFailures >= config.UnhealthyThreshold)
            {
                _logger.LogWarning("Process {Id} health check failed {Count} times, requesting restart",
                    definitionId, state.ConsecutiveFailures);
                state.ConsecutiveFailures = 0;
                RestartRequested?.Invoke(this, definitionId);
            }
        }
    }

    private async Task<bool> CheckHttpAsync(HealthCheckConfig config)
    {
        if (string.IsNullOrEmpty(config.Url)) return true;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.TimeoutSeconds));
        var response = await _httpClient.GetAsync(config.Url, cts.Token);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> CheckTcpAsync(HealthCheckConfig config)
    {
        using var client = new TcpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.TimeoutSeconds));
        await client.ConnectAsync("localhost", config.Port, cts.Token);
        return true;
    }

    public void Dispose()
    {
        foreach (var state in _checks.Values)
            state.Timer?.Dispose();
        _checks.Clear();
        _httpClient.Dispose();
    }

    private class HealthCheckState
    {
        public ProcessDefinition Definition { get; set; } = null!;
        public Timer? Timer { get; set; }
        public int ConsecutiveFailures { get; set; }
    }
}
