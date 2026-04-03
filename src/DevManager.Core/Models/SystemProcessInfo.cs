namespace DevManager.Core.Models;

public class SystemProcessInfo
{
    public int Pid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CommandLine { get; set; } = string.Empty;
    public List<int> Ports { get; set; } = [];
    public string? WorkingDirectory { get; set; }
    public double MemoryMb { get; set; }

    /// <summary>
    /// Bu process hangi kayıtlı projeye ait (eşleşme varsa).
    /// </summary>
    public string? MatchedProjectName { get; set; }

    /// <summary>
    /// Port listesini virgülle ayrılmış string olarak döndürür.
    /// </summary>
    public string PortsDisplay => Ports.Count > 0 ? string.Join(", ", Ports) : "—";
}
