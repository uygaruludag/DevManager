using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using DevManager.Core.Models;
using DevManager.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevManager.Core.Services;

public class ProcessManagerService : IProcessManagerService, IDisposable
{
    private readonly ILogService _logService;
    private readonly ILogger<ProcessManagerService> _logger;
    private readonly ConcurrentDictionary<Guid, ManagedProcess> _processes = new();
    private readonly ConcurrentDictionary<Guid, ProcessDefinition> _definitions = new();

    public event EventHandler<ProcessInstance>? ProcessStateChanged;

    public IReadOnlyDictionary<Guid, ProcessInstance> Instances =>
        _processes.ToDictionary(kv => kv.Key, kv => kv.Value.Instance);

    public ProcessManagerService(ILogService logService, ILogger<ProcessManagerService> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    public ProcessInstance? GetInstance(Guid definitionId)
    {
        return _processes.TryGetValue(definitionId, out var mp) ? mp.Instance : null;
    }

    public Process? GetSystemProcess(Guid definitionId)
    {
        if (!_processes.TryGetValue(definitionId, out var mp))
            return null;

        try
        {
            if (mp.Process != null && !mp.Process.HasExited)
                return mp.Process;
        }
        catch { }

        return null;
    }

    public async Task StartProcessAsync(ProcessDefinition definition)
    {
        if (_processes.TryGetValue(definition.Id, out var existing) &&
            existing.Instance.State is ProcessState.Running or ProcessState.Starting)
        {
            return;
        }

        _definitions[definition.Id] = definition;

        var instance = new ProcessInstance
        {
            DefinitionId = definition.Id,
            State = ProcessState.Starting
        };

        var managed = new ManagedProcess { Instance = instance };
        _processes[definition.Id] = managed;
        RaiseStateChanged(instance);

        _logService.AppendLog(definition.Id, $"Starting: {definition.Command} {definition.Arguments}", LogEntryType.System);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = definition.Command,
                Arguments = definition.Arguments,
                WorkingDirectory = definition.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            foreach (var env in definition.EnvironmentVariables)
                startInfo.EnvironmentVariables[env.Key] = env.Value;

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    _logService.AppendLog(definition.Id, e.Data, LogEntryType.StdOut);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    _logService.AppendLog(definition.Id, e.Data, LogEntryType.StdErr);
            };

            process.Exited += (_, _) => OnProcessExited(definition.Id);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            managed.Process = process;
            instance.ProcessId = process.Id;
            instance.StartedAt = DateTime.Now;
            instance.State = ProcessState.Running;

            _logService.AppendLog(definition.Id, $"Process started (PID: {process.Id})", LogEntryType.System);
            RaiseStateChanged(instance);
        }
        catch (Exception ex)
        {
            instance.State = ProcessState.Crashed;
            instance.LastError = ex.Message;
            _logService.AppendLog(definition.Id, $"Failed to start: {ex.Message}", LogEntryType.System);
            RaiseStateChanged(instance);
            _logger.LogError(ex, "Failed to start process {Name}", definition.Name);
        }
    }

    public async Task StopProcessAsync(Guid definitionId, bool force = false)
    {
        if (!_processes.TryGetValue(definitionId, out var managed))
            return;

        var process = managed.Process;
        if (process == null)
        {
            // Process objesi yok ama state Running olabilir (temizle)
            if (managed.Instance.State is not ProcessState.Stopped)
            {
                managed.Instance.State = ProcessState.Stopped;
                managed.Instance.ProcessId = null;
                RaiseStateChanged(managed.Instance);
            }
            return;
        }

        var instance = managed.Instance;
        if (instance.State is ProcessState.Stopped or ProcessState.Stopping)
            return;

        instance.State = ProcessState.Stopping;
        RaiseStateChanged(instance);
        _logService.AppendLog(definitionId, "Process durduruluyor...", LogEntryType.System);

        managed.IsStoppingManually = true;

        try
        {
            // Process hâlâ çalışıyor mu kontrol et
            bool hasExited;
            try { hasExited = process.HasExited; }
            catch { hasExited = true; }

            if (hasExited)
            {
                CompleteStop(managed, definitionId);
                return;
            }

            if (!force)
            {
                // Graceful shutdown denemesi: stdin'e Ctrl+C karakteri yaz
                var graceful = TrySendCtrlCViaStdin(managed);
                if (graceful)
                {
                    var exited = await WaitForExitSafeAsync(process, TimeSpan.FromSeconds(5));
                    if (exited)
                    {
                        CompleteStop(managed, definitionId);
                        return;
                    }
                }
            }

            // Force kill
            ForceKillProcess(process);
            await WaitForExitSafeAsync(process, TimeSpan.FromSeconds(5));
            CompleteStop(managed, definitionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Process durdurulurken hata {Id}", definitionId);
            instance.State = ProcessState.Stopped;
            instance.StoppedAt = DateTime.Now;
            instance.ProcessId = null;
            managed.IsStoppingManually = false;
            RaiseStateChanged(instance);
        }
    }

    private static void ForceKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch { }
        }
    }

    private static async Task<bool> WaitForExitSafeAsync(Process process, TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task RestartProcessAsync(Guid definitionId)
    {
        if (!_definitions.TryGetValue(definitionId, out var definition))
            return;

        await StopProcessAsync(definitionId, force: false);
        await Task.Delay(500);
        await StartProcessAsync(definition);
    }

    public async Task StartProjectAsync(Project project)
    {
        var ordered = project.Processes
            .Where(p => p.AutoStartWithProject)
            .OrderBy(p => p.SortOrder)
            .ToList();

        // Group by StartupDelaySeconds: start all with same delay concurrently
        var tasks = ordered.Select(async process =>
        {
            if (process.StartupDelaySeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(process.StartupDelaySeconds));

            await StartProcessAsync(process);
        });

        await Task.WhenAll(tasks);
    }

    public async Task StopProjectAsync(Project project, bool force = false)
    {
        foreach (var proc in project.Processes)
        {
            try
            {
                await StopProcessAsync(proc.Id, force);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "StopProject sırasında hata: {Id}", proc.Id);
            }
        }
    }

    public async Task StopAllAsync(bool force = false)
    {
        // Sıralı durdur - Ctrl+C paralel çalışamaz (console attach/detach race condition)
        var ids = _processes.Keys.ToList();
        foreach (var id in ids)
        {
            try
            {
                await StopProcessAsync(id, force);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "StopAll sırasında hata: {Id}", id);
            }
        }
    }

    private void OnProcessExited(Guid definitionId)
    {
        try
        {
            if (!_processes.TryGetValue(definitionId, out var managed))
                return;

            if (managed.IsStoppingManually)
                return;

            int exitCode;
            try { exitCode = managed.Process?.HasExited == true ? managed.Process.ExitCode : -1; }
            catch { exitCode = -1; }

            var instance = managed.Instance;

            instance.LastExitCode = exitCode;
            instance.StoppedAt = DateTime.Now;
            instance.ProcessId = null;

            if (exitCode == 0)
            {
                instance.State = ProcessState.Stopped;
                _logService.AppendLog(definitionId, "Process normal şekilde kapandı (code: 0)", LogEntryType.System);
            }
            else
            {
                instance.State = ProcessState.Crashed;
                _logService.AppendLog(definitionId, $"Process kapandı (exit code: {exitCode})", LogEntryType.System);

                // Auto-restart logic
                if (_definitions.TryGetValue(definitionId, out var definition) && definition.AutoRestartOnCrash)
                {
                    _ = HandleAutoRestartAsync(definitionId, definition, instance);
                }
            }

            RaiseStateChanged(instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnProcessExited hatası: {Id}", definitionId);
        }
    }

    private async Task HandleAutoRestartAsync(Guid definitionId, ProcessDefinition definition, ProcessInstance instance)
    {
        // Reset restart count if outside the window
        if (instance.LastRestartWindowStart == null ||
            (DateTime.Now - instance.LastRestartWindowStart.Value).TotalMinutes > definition.RestartWindowMinutes)
        {
            instance.RestartCount = 0;
            instance.LastRestartWindowStart = DateTime.Now;
        }

        if (instance.RestartCount >= definition.MaxRestartAttempts)
        {
            _logService.AppendLog(definitionId,
                $"Max restart attempts ({definition.MaxRestartAttempts}) reached. Not restarting.",
                LogEntryType.System);
            return;
        }

        instance.RestartCount++;
        instance.State = ProcessState.Restarting;
        RaiseStateChanged(instance);

        _logService.AppendLog(definitionId,
            $"Auto-restarting in {definition.RestartDelaySeconds}s (attempt {instance.RestartCount}/{definition.MaxRestartAttempts})",
            LogEntryType.System);

        await Task.Delay(TimeSpan.FromSeconds(definition.RestartDelaySeconds));

        await StartProcessAsync(definition);
    }

    private void CompleteStop(ManagedProcess managed, Guid definitionId)
    {
        var instance = managed.Instance;
        instance.State = ProcessState.Stopped;
        instance.StoppedAt = DateTime.Now;
        instance.ProcessId = null;
        managed.IsStoppingManually = false;

        _logService.AppendLog(definitionId, "Process durduruldu", LogEntryType.System);
        RaiseStateChanged(instance);

        try { managed.Process?.Dispose(); } catch { }
        managed.Process = null;
    }

    private void RaiseStateChanged(ProcessInstance instance)
    {
        try
        {
            ProcessStateChanged?.Invoke(this, instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RaiseStateChanged hatası: {Id}", instance.DefinitionId);
        }
    }

    /// <summary>
    /// Stdin redirect edilmiş process'e Ctrl+C (0x03) karakteri gönder.
    /// Bu yöntem sadece DevManager'ın başlattığı process'ler için çalışır (stdin redirect edilmiş).
    /// Orphan/adopted process'ler için çalışmaz ama ForceKill devreye girer.
    /// </summary>
    private static bool TrySendCtrlCViaStdin(ManagedProcess managed)
    {
        try
        {
            if (managed.Process?.StartInfo.RedirectStandardInput == true &&
                managed.Process?.HasExited == false)
            {
                managed.Process.StandardInput.Write("\x3"); // Ctrl+C = 0x03
                managed.Process.StandardInput.Flush();
                return true;
            }
        }
        catch { }
        return false;
    }

    #region Orphan Process Detection

    public async Task<int> DetectAndAdoptOrphansAsync(IEnumerable<ProcessDefinition> definitions)
    {
        var adoptedCount = 0;
        var defList = definitions.ToList();

        try
        {
            // WMI ile tüm çalışan processlerin CommandLine bilgisini al
            var runningProcesses = await Task.Run(() => GetRunningProcessesWithCommandLine());

            foreach (var definition in defList)
            {
                // Zaten bu definition için çalışan process varsa atla
                if (_processes.TryGetValue(definition.Id, out var existing) &&
                    existing.Instance.State is ProcessState.Running or ProcessState.Starting)
                    continue;

                // Bu definition'a uyan bir OS process bul
                var matchedPid = FindMatchingProcess(definition, runningProcesses);
                if (matchedPid == null) continue;

                try
                {
                    var process = Process.GetProcessById(matchedPid.Value);
                    if (process.HasExited) continue;

                    // Process'i sahiplen
                    AttachToProcess(definition, process);
                    adoptedCount++;

                    _logService.AppendLog(definition.Id,
                        $"Önceki oturumdan çalışan process bulundu ve sahiplenildi (PID: {matchedPid.Value})",
                        LogEntryType.System);
                    _logService.AppendLog(definition.Id,
                        "Canlı log bu oturum için kullanılamaz - process yeniden başlatılırsa loglar görünür",
                        LogEntryType.System);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Orphan process {PID} sahiplenirken hata", matchedPid.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orphan process tespitinde hata");
        }

        return adoptedCount;
    }

    private void AttachToProcess(ProcessDefinition definition, Process process)
    {
        _definitions[definition.Id] = definition;

        var instance = new ProcessInstance
        {
            DefinitionId = definition.Id,
            State = ProcessState.Running,
            ProcessId = process.Id,
            StartedAt = GetProcessStartTime(process)
        };

        var managed = new ManagedProcess
        {
            Instance = instance,
            Process = process
        };

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => OnProcessExited(definition.Id);

        _processes[definition.Id] = managed;
        RaiseStateChanged(instance);
    }

    private static DateTime? GetProcessStartTime(Process process)
    {
        try { return process.StartTime; }
        catch { return null; }
    }

    private static List<(int Pid, string CommandLine)> GetRunningProcessesWithCommandLine()
    {
        var result = new List<(int, string)>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT ProcessId, CommandLine FROM Win32_Process WHERE CommandLine IS NOT NULL");

        foreach (ManagementObject obj in searcher.Get())
        {
            var pid = Convert.ToInt32(obj["ProcessId"]);
            var cmdLine = obj["CommandLine"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(cmdLine))
                result.Add((pid, cmdLine));
        }

        return result;
    }

    private static int? FindMatchingProcess(
        ProcessDefinition definition,
        List<(int Pid, string CommandLine)> runningProcesses)
    {
        var command = definition.Command.ToLowerInvariant();
        var args = definition.Arguments.ToLowerInvariant();
        var workDir = definition.WorkingDirectory.Replace('/', '\\').ToLowerInvariant();

        foreach (var (pid, cmdLine) in runningProcesses)
        {
            var cmdLower = cmdLine.ToLowerInvariant();

            if (command == "dotnet")
            {
                // dotnet processleri: CommandLine içinde csproj path'ini ara
                // Örn: dotnet run --project "D:\source\siteron\src\Siteron.Api\Siteron.Api.csproj"
                if (cmdLower.Contains("dotnet") && !string.IsNullOrEmpty(args))
                {
                    // Arguments içindeki csproj path'ini extract et
                    var csprojMatch = ExtractCsprojPath(args);
                    if (!string.IsNullOrEmpty(csprojMatch) && cmdLower.Contains(csprojMatch))
                        return pid;

                    // Alternatif: tam argüman eşleşmesi
                    if (cmdLower.Contains(args.Replace("\"", "")))
                        return pid;
                }
            }
            else if (command == "cmd")
            {
                // npm processleri: cmd /c npm run dev gibi
                // Working directory + npm komutu birlikte kontrol et
                if (cmdLower.Contains("npm") && cmdLower.Contains("run"))
                {
                    // Node process'lerin parent'ı cmd olabilir, veya doğrudan node
                    // WorkingDirectory kontrolü için process'in cwd'sini bilemeyiz
                    // Ama CommandLine içinde path ipuçları olabilir
                    if (cmdLower.Contains(workDir))
                        return pid;
                }
            }
            else
            {
                // Genel eşleşme: command + arguments
                if (cmdLower.Contains(command) && !string.IsNullOrEmpty(args) && cmdLower.Contains(args))
                    return pid;
            }
        }

        // Dotnet için ek arama: node/npm processleri working directory ile eşleştir
        if (command == "cmd" && args.Contains("npm"))
        {
            foreach (var (pid, cmdLine) in runningProcesses)
            {
                var cmdLower = cmdLine.ToLowerInvariant();
                // node process working directory içeriyorsa
                if (cmdLower.Contains("node") && cmdLower.Contains(workDir))
                    return pid;
            }
        }

        return null;
    }

    private static string ExtractCsprojPath(string arguments)
    {
        // "--project "D:\...\Foo.csproj"" veya "--project D:\...\Foo.csproj" formatından path al
        var projectIdx = arguments.IndexOf("--project", StringComparison.OrdinalIgnoreCase);
        if (projectIdx < 0) return string.Empty;

        var afterProject = arguments[(projectIdx + "--project".Length)..].Trim();
        // Tırnak varsa tırnak içini al, yoksa boşluğa kadar al
        if (afterProject.StartsWith('"'))
        {
            var endQuote = afterProject.IndexOf('"', 1);
            return endQuote > 0
                ? afterProject[1..endQuote].Replace("\\", "\\").ToLowerInvariant()
                : afterProject[1..].ToLowerInvariant();
        }

        var spaceIdx = afterProject.IndexOf(' ');
        return (spaceIdx > 0 ? afterProject[..spaceIdx] : afterProject).ToLowerInvariant();
    }

    #endregion

    public void Dispose()
    {
        foreach (var mp in _processes.Values)
        {
            try
            {
                if (mp.Process != null && !mp.Process.HasExited)
                    mp.Process.Kill(entireProcessTree: true);
                mp.Process?.Dispose();
            }
            catch { }
        }
        _processes.Clear();
    }

    private class ManagedProcess
    {
        public Process? Process { get; set; }
        public ProcessInstance Instance { get; set; } = new();
        public bool IsStoppingManually { get; set; }
    }
}
