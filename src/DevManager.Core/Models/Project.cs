namespace DevManager.Core.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#2196F3";
    public int SortOrder { get; set; }
    public bool AutoStartOnLaunch { get; set; }
    public List<ProcessDefinition> Processes { get; set; } = [];
}
