namespace DevManager.Core.Models;

public class AppSettings
{
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartMinimized { get; set; }
    public bool LaunchAtWindowsStartup { get; set; }
    public int MaxLogLinesPerProcess { get; set; } = 5000;
    public bool ConfirmBeforeStopAll { get; set; } = true;
    public string Theme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "Blue";
    public string Language { get; set; } = "tr";
}
