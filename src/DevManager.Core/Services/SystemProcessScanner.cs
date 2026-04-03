using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using DevManager.Core.Models;

namespace DevManager.Core.Services;

public static class SystemProcessScanner
{
    private static readonly HashSet<string> TargetProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node", "dotnet", "npm", "java", "python", "python3", "ruby", "php", "go"
    };

    /// <summary>
    /// Sistemdeki geliştirme süreçlerini (node, dotnet, npm vb.) port bilgileriyle birlikte tarar.
    /// Kayıtlı projelerin dizinleriyle eşleştirme yapar.
    /// </summary>
    public static List<SystemProcessInfo> Scan(IEnumerable<Project> projects)
    {
        var result = new List<SystemProcessInfo>();
        var projectPaths = projects
            .SelectMany(p => p.Processes.Select(proc => new
            {
                ProjectName = p.Name,
                WorkDir = proc.WorkingDirectory.Replace('/', '\\').TrimEnd('\\').ToLowerInvariant()
            }))
            .Where(x => !string.IsNullOrEmpty(x.WorkDir))
            .ToList();

        // Port → PID mapping
        var portMap = GetPortToPidMap();

        // WMI ile process bilgilerini al
        var wmiProcesses = GetWmiProcesses();

        foreach (var (pid, processName, cmdLine) in wmiProcesses)
        {
            // Sadece dev process'leri filtrele
            if (!IsDevProcess(processName, cmdLine))
                continue;

            // DevManager kendisi olmasın
            if (pid == Environment.ProcessId)
                continue;

            var info = new SystemProcessInfo
            {
                Pid = pid,
                Name = processName,
                CommandLine = cmdLine
            };

            // Port eşleştirme
            if (portMap.TryGetValue(pid, out var ports))
                info.Ports = ports;

            // Memory
            try
            {
                var proc = Process.GetProcessById(pid);
                info.MemoryMb = proc.WorkingSet64 / 1024.0 / 1024.0;
            }
            catch { }

            // Proje eşleştirme: CommandLine içinde proje dizini geçiyor mu?
            var cmdLower = cmdLine.ToLowerInvariant();
            var match = projectPaths.FirstOrDefault(p => cmdLower.Contains(p.WorkDir));
            if (match != null)
                info.MatchedProjectName = match.ProjectName;

            result.Add(info);
        }

        return result.OrderBy(p => p.MatchedProjectName == null)
                     .ThenBy(p => p.MatchedProjectName)
                     .ThenBy(p => p.Name)
                     .ToList();
    }

    /// <summary>
    /// PID'ye göre process'i sonlandırır (process tree dahil).
    /// </summary>
    public static bool KillProcess(int pid)
    {
        try
        {
            var proc = Process.GetProcessById(pid);
            proc.Kill(entireProcessTree: true);
            return true;
        }
        catch
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
                return true;
            }
            catch { return false; }
        }
    }

    private static bool IsDevProcess(string processName, string cmdLine)
    {
        var nameLower = processName.ToLowerInvariant();

        // Doğrudan hedef process ismi
        if (TargetProcessNames.Contains(nameLower))
            return true;

        // cmd/powershell ile çalıştırılan npm/node
        if (nameLower is "cmd" or "powershell" or "pwsh")
        {
            var cmdLower = cmdLine.ToLowerInvariant();
            return cmdLower.Contains("npm") || cmdLower.Contains("node") ||
                   cmdLower.Contains("dotnet") || cmdLower.Contains("yarn") ||
                   cmdLower.Contains("pnpm");
        }

        return false;
    }

    private static List<(int Pid, string Name, string CmdLine)> GetWmiProcesses()
    {
        var result = new List<(int, string, string)>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, Name, CommandLine FROM Win32_Process WHERE CommandLine IS NOT NULL");

            foreach (ManagementObject obj in searcher.Get())
            {
                var pid = Convert.ToInt32(obj["ProcessId"]);
                var name = obj["Name"]?.ToString()?.Replace(".exe", "") ?? "";
                var cmdLine = obj["CommandLine"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(cmdLine))
                    result.Add((pid, name, cmdLine));
            }
        }
        catch { }

        return result;
    }

    private static Dictionary<int, List<int>> GetPortToPidMap()
    {
        var map = new Dictionary<int, List<int>>();

        try
        {
            // netstat -ano ile port → PID mapping
            var psi = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return map;

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.Contains("LISTENING")) continue;

                var parts = trimmed.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;

                var localAddress = parts[1];
                var colonIdx = localAddress.LastIndexOf(':');
                if (colonIdx < 0) continue;

                if (int.TryParse(localAddress[(colonIdx + 1)..], out var port) &&
                    int.TryParse(parts[^1], out var pid) &&
                    port > 0)
                {
                    if (!map.ContainsKey(pid))
                        map[pid] = [];
                    if (!map[pid].Contains(port))
                        map[pid].Add(port);
                }
            }
        }
        catch { }

        return map;
    }
}
