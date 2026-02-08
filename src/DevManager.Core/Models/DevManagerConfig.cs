namespace DevManager.Core.Models;

public class DevManagerConfig
{
    public int Version { get; set; } = 1;
    public AppSettings Settings { get; set; } = new();
    public List<Project> Projects { get; set; } = [];
}
