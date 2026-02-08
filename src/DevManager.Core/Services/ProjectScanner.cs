using System.Text.Json;
using System.Xml.Linq;
using DevManager.Core.Models;

namespace DevManager.Core.Services;

public static class ProjectScanner
{
    public static List<ProcessDefinition> ScanDirectory(string rootPath)
    {
        var results = new List<ProcessDefinition>();

        if (!Directory.Exists(rootPath))
            return results;

        // Find .csproj files (skip test/obj/bin directories)
        var csprojFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !IsExcludedPath(f))
            .ToList();

        foreach (var csproj in csprojFiles)
        {
            var def = AnalyzeCsproj(csproj);
            if (def != null)
                results.Add(def);
        }

        // Find package.json files (skip node_modules)
        var packageJsonFiles = Directory.GetFiles(rootPath, "package.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules") && !IsExcludedPath(f))
            .ToList();

        foreach (var pkg in packageJsonFiles)
        {
            var def = AnalyzePackageJson(pkg);
            if (def != null)
                results.Add(def);
        }

        // Sort: APIs first, then frontends
        return results
            .OrderBy(d => d.Name.Contains("Frontend") || d.Name.Contains("Web") ? 1 : 0)
            .Select((d, i) => { d.SortOrder = i; return d; })
            .ToList();
    }

    private static ProcessDefinition? AnalyzeCsproj(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            var outputType = doc.Descendants(ns + "OutputType").FirstOrDefault()?.Value ?? "";
            var sdk = doc.Root?.Attribute("Sdk")?.Value ?? "";

            // Check if it's a web/API project
            bool isWeb = sdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);
            bool isExe = outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase)
                      || outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase);

            // Skip class libraries and test projects
            var projectName = Path.GetFileNameWithoutExtension(csprojPath);
            if (!isWeb && !isExe)
                return null;

            if (IsTestProject(projectName, csprojPath))
                return null;

            var workingDir = Path.GetDirectoryName(csprojPath)!;

            // Detect launch profile for port info
            var launchProfile = DetectLaunchProfile(workingDir);

            var displayName = DeriveDisplayName(projectName, isWeb);
            var arguments = $"run --project \"{csprojPath}\"";

            if (!string.IsNullOrEmpty(launchProfile.ProfileName))
                arguments += $" --launch-profile \"{launchProfile.ProfileName}\"";

            return new ProcessDefinition
            {
                Name = displayName,
                Command = "dotnet",
                Arguments = arguments,
                WorkingDirectory = FindSolutionDir(csprojPath) ?? workingDir,
                AutoRestartOnCrash = true,
                MaxRestartAttempts = 3,
                RestartDelaySeconds = 5,
                HealthCheck = !string.IsNullOrEmpty(launchProfile.Url) ? new HealthCheckConfig
                {
                    Type = HealthCheckType.HttpEndpoint,
                    Url = launchProfile.Url.TrimEnd('/') + "/health",
                    IntervalSeconds = 30,
                    TimeoutSeconds = 5,
                    UnhealthyThreshold = 3
                } : null
            };
        }
        catch
        {
            return null;
        }
    }

    private static ProcessDefinition? AnalyzePackageJson(string packageJsonPath)
    {
        try
        {
            var json = File.ReadAllText(packageJsonPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Must have scripts.dev
            if (!root.TryGetProperty("scripts", out var scripts))
                return null;

            if (!scripts.TryGetProperty("dev", out _))
                return null;

            var workingDir = Path.GetDirectoryName(packageJsonPath)!;
            var folderName = Path.GetFileName(workingDir);

            // Detect project type
            var projectType = DetectFrontendType(root);
            var displayName = $"{folderName} ({projectType})";

            // Detect port from scripts
            var devScript = scripts.GetProperty("dev").GetString() ?? "";
            var port = ExtractPort(devScript);

            return new ProcessDefinition
            {
                Name = displayName,
                Command = "cmd",
                Arguments = "/c npm run dev",
                WorkingDirectory = workingDir,
                AutoRestartOnCrash = true,
                MaxRestartAttempts = 2,
                RestartDelaySeconds = 3,
                HealthCheck = port > 0 ? new HealthCheckConfig
                {
                    Type = HealthCheckType.HttpEndpoint,
                    Url = $"http://localhost:{port}",
                    IntervalSeconds = 60,
                    TimeoutSeconds = 10,
                    UnhealthyThreshold = 2
                } : null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string DetectFrontendType(JsonElement root)
    {
        if (root.TryGetProperty("dependencies", out var deps))
        {
            if (deps.TryGetProperty("next", out _)) return "Next.js";
            if (deps.TryGetProperty("nuxt", out _)) return "Nuxt";
            if (deps.TryGetProperty("react-native", out _)) return "React Native";
            if (deps.TryGetProperty("expo", out _)) return "Expo";
            if (deps.TryGetProperty("react", out _))
            {
                if (root.TryGetProperty("devDependencies", out var devDeps))
                {
                    if (devDeps.TryGetProperty("vite", out _)) return "React/Vite";
                }
                return "React";
            }
            if (deps.TryGetProperty("vue", out _)) return "Vue";
            if (deps.TryGetProperty("@angular/core", out _)) return "Angular";
        }
        return "Frontend";
    }

    private static int ExtractPort(string script)
    {
        // Match patterns like --port 3000, -p 4000, PORT=3000
        var portPatterns = new[]
        {
            @"--port\s+(\d+)",
            @"-p\s+(\d+)",
            @"PORT[=\s]+(\d+)",
            @"port\s+(\d+)"
        };

        foreach (var pattern in portPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(script, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
                return port;
        }

        return 0;
    }

    private static (string ProfileName, string Url) DetectLaunchProfile(string projectDir)
    {
        var launchSettingsPath = Path.Combine(projectDir, "Properties", "launchSettings.json");
        if (!File.Exists(launchSettingsPath))
            return (string.Empty, string.Empty);

        try
        {
            var json = File.ReadAllText(launchSettingsPath);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("profiles", out var profiles))
                return (string.Empty, string.Empty);

            // Find first project profile (not IIS Express)
            foreach (var profile in profiles.EnumerateObject())
            {
                if (profile.Value.TryGetProperty("commandName", out var cmd) &&
                    cmd.GetString() == "Project")
                {
                    var url = string.Empty;
                    if (profile.Value.TryGetProperty("applicationUrl", out var appUrl))
                        url = appUrl.GetString()?.Split(';').FirstOrDefault() ?? "";

                    return (profile.Name, url);
                }
            }

            // Fallback: first http profile
            foreach (var profile in profiles.EnumerateObject())
            {
                var name = profile.Name.ToLower();
                if (name.Contains("http") && !name.Contains("iis"))
                {
                    var url = string.Empty;
                    if (profile.Value.TryGetProperty("applicationUrl", out var appUrl))
                        url = appUrl.GetString()?.Split(';').FirstOrDefault() ?? "";

                    return (profile.Name, url);
                }
            }
        }
        catch { }

        return (string.Empty, string.Empty);
    }

    private static string DeriveDisplayName(string projectName, bool isWeb)
    {
        // "Siteron.Api" -> "API Backend"
        // "SiteronPay.Api" -> "API Backend"
        // "SiteronEsnaf.API" -> "API Backend"
        var lower = projectName.ToLower();

        if (lower.EndsWith(".api") || lower.EndsWith("api"))
            return "API Backend";
        if (lower.Contains("worker") || lower.Contains("job"))
            return "Background Worker";
        if (lower.Contains("gateway"))
            return "API Gateway";

        if (isWeb)
            return $"{projectName} (Web)";

        return projectName;
    }

    private static bool IsTestProject(string name, string csprojPath)
    {
        var lower = name.ToLower();
        if (lower.Contains("test") || lower.Contains("spec") || lower.Contains("benchmark"))
            return true;

        // Check for test framework references
        try
        {
            var content = File.ReadAllText(csprojPath);
            if (content.Contains("Microsoft.NET.Test.Sdk") || content.Contains("xunit") || content.Contains("NUnit"))
                return true;
        }
        catch { }

        return false;
    }

    private static bool IsExcludedPath(string path)
    {
        var normalized = path.Replace('\\', '/').ToLower();
        return normalized.Contains("/obj/")
            || normalized.Contains("/bin/")
            || normalized.Contains("/node_modules/")
            || normalized.Contains("/.git/");
    }

    private static string? FindSolutionDir(string csprojPath)
    {
        var dir = Path.GetDirectoryName(csprojPath);
        while (dir != null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
