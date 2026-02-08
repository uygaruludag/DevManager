namespace DevManager.Core.Models;

public class HealthCheckConfig
{
    public HealthCheckType Type { get; set; } = HealthCheckType.ProcessRunning;
    public string? Url { get; set; }
    public int Port { get; set; }
    public int IntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 5;
    public int UnhealthyThreshold { get; set; } = 3;
}

public enum HealthCheckType
{
    ProcessRunning,
    HttpEndpoint,
    TcpPort
}
