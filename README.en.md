<p align="center">
  <a href="README.md">🇹🇷 Türkçe</a> |
  <a href="README.en.md">🇬🇧 English</a> |
  <a href="README.de.md">🇩🇪 Deutsch</a> |
  <a href="README.fr.md">🇫🇷 Français</a>
</p>

<p align="center">
  <img src="ScreenShot.png" alt="DevManager Screenshot" width="800"/>
</p>

<h1 align="center">DevManager</h1>

<p align="center">
  <b>Manage multiple development processes from a single interface.</b><br/>
  .NET 8 | WPF | Material Design 3
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#license">License</a>
</p>

---

## What is it?

**DevManager** is a Windows desktop application designed for developers running multiple microservices, APIs, frontend applications, or background services. Organize all your processes by project, and start, stop, restart, and monitor live logs from a single panel.

---

<a id="features"></a>
## Features

### Process Management
- **Batch Start / Stop** - Control all processes or per-project with a single click
- **Graceful Shutdown** - Clean shutdown via Ctrl+C signal, force kill if necessary
- **Auto Restart** - Automatically restart crashed processes with configurable policies
- **Orphan Process Detection** - Automatically detect and adopt running processes from previous sessions

### Project Organization
- **Project Groups** - Organize your processes by projects (color-coded)
- **Auto Project Scanning** - Automatically detect .csproj and package.json files from folder path
- **Framework Detection** - Automatic recognition of React, Vue, Angular, Next.js, Nuxt, etc.

### Monitoring
- **Live Log Streaming** - Real-time stdout/stderr output monitoring
- **Health Checks** - Periodic health checks via HTTP endpoint or TCP port
- **Process Status** - Instant display of PID, uptime, restart count
- **CPU/RAM Metrics** - Per-process CPU usage and memory consumption display

### Interface
- **Material Design 3** - Modern, dark theme design
- **Performant Log Display** - Virtualized list, batched updates (100ms batch)
- **Circular Log Buffer** - Memory protection with 5000-line limit per process
- **Multi-Language Support** - Turkish, English, German, French

---

<a id="installation"></a>
## Installation

### Download (Recommended)

> **No .NET installation required** — Self-contained, single file.

| Platform | Download |
|----------|----------|
| Windows x64 | [DevManager-v1.2.0-win-x64.zip](https://github.com/uygaruludag/DevManager/releases/download/v1.2.0/DevManager-v1.2.0-win-x64.zip) |

1. Download the ZIP file and extract it to any folder
2. Run `DevManager.App.exe`
3. That's it!

> ⚠️ **Windows SmartScreen Warning:** On first launch, you may see a "Windows protected your PC" warning. This is because the app is not yet digitally signed (code signing certificate). Click **"More info" → "Run anyway"** to proceed safely.

### Build from Source

#### Requirements
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
# Clone the repository
git clone https://github.com/uygaruludag/DevManager.git
cd DevManager

# Build
dotnet build DevManager.sln

# Run
dotnet run --project src/DevManager.App/DevManager.App.csproj
```

### Self-Contained Publish

```bash
dotnet publish src/DevManager.App/DevManager.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

<a id="usage"></a>
## Usage

### Adding a Project

1. Click the **"+ Add Project"** button in the bottom-left corner
2. Enter the project folder path and click **"Scan"**
3. Select the desired processes from the automatically detected ones
4. Configure the project name, color, and auto-start settings
5. Save with **"Add Project"**

### Process Control

| Action | Description |
|--------|-------------|
| ▶ Play | Start the process |
| ■ Stop | Stop the process (graceful → force) |
| ↻ Restart | Restart the process |
| 🗑 Clear | Clear the log display |
| 📋 Copy | Copy logs to clipboard |

### Batch Operations
- **Top toolbar**: Start/stop all processes across all projects
- **Project header**: Start/stop/restart all processes in the selected project

---

<a id="architecture"></a>
## Architecture

```
DevManager.sln
├── src/
│   ├── DevManager.Core/           # Business logic layer
│   │   ├── Models/                # Data models
│   │   │   ├── DevManagerConfig   # Main configuration
│   │   │   ├── Project            # Project definition
│   │   │   ├── ProcessDefinition  # Process definition
│   │   │   ├── ProcessInstance    # Runtime state
│   │   │   ├── HealthCheckConfig  # Health check settings
│   │   │   └── LogEntry           # Log entry
│   │   └── Services/              # Services
│   │       ├── ProcessManagerService    # Process lifecycle
│   │       ├── LogService               # Circular log buffer
│   │       ├── ConfigurationService     # JSON configuration
│   │       ├── HealthCheckService       # HTTP/TCP health checks
│   │       └── ProjectScanner           # Auto project scanning
│   │
│   ├── DevManager.App/            # WPF UI layer
│   │   ├── ViewModels/            # MVVM ViewModels
│   │   ├── Views/                 # XAML views
│   │   └── Resources/             # Converters, styles, localization
│   │
│   └── DevManager.Infrastructure/ # Platform-dependent layer
│       ├── SystemTray              # System tray
│       └── JsonConfigStore         # JSON storage
```

### Technology Stack

| Technology | Usage |
|------------|-------|
| .NET 8 (WPF) | Desktop application framework |
| MaterialDesignThemes 5.1 | Material Design 3 UI |
| CommunityToolkit.Mvvm 8.4 | MVVM infrastructure |
| Microsoft.Extensions.DI | Dependency injection |
| System.Management (WMI) | Orphan process detection |
| Hardcodet.NotifyIcon.Wpf | System tray (planned) |

### Design Decisions

- **Graceful Shutdown**: First Ctrl+C via stdin, force kill on failure
- **Circular Buffer**: Limited log per process (default 5000 lines) for memory control
- **Batched UI Updates**: Batch log updates every 100ms to prevent UI freezing
- **ConcurrentDictionary**: Thread-safe process management
- **Event-Driven**: State changes communicated to UI via events
- **Atomic File Writes**: Configuration written to temp file then moved

---

<a id="configuration"></a>
## Configuration

The configuration file is stored at `%APPDATA%\DevManager\devmanager-config.json`.

### Process Settings

```json
{
  "name": "API Backend",
  "command": "dotnet",
  "arguments": "run --project ./src/Api.csproj",
  "workingDirectory": "D:\\source\\project",
  "autoRestartOnCrash": true,
  "maxRestartAttempts": 3,
  "restartDelaySeconds": 5,
  "healthCheck": {
    "type": "httpEndpoint",
    "url": "http://localhost:5000/health",
    "intervalSeconds": 30,
    "timeoutSeconds": 5,
    "unhealthyThreshold": 3
  }
}
```

### Application Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `maxLogLinesPerProcess` | 5000 | Maximum log lines per process |
| `theme` | Dark | Theme (Dark / Light) |
| `language` | tr | Language (tr, en, de, fr) |
| `confirmBeforeStopAll` | true | Confirm before batch stop |
| `minimizeToTrayOnClose` | true | Minimize to tray on close |
| `startMinimized` | false | Start minimized |

---

## Roadmap

- [x] Process management (start/stop/restart)
- [x] Project organization
- [x] Live log monitoring
- [x] Auto restart
- [x] Orphan process detection
- [x] Auto project scanning
- [x] CPU/RAM metrics (per-process)
- [x] Multi-language support (TR/EN/DE/FR)
- [ ] System tray integration
- [ ] Settings UI
- [ ] Config export/import
- [ ] Single-instance control

---

<a id="license"></a>
## License

This project is developed by [Istech Yazılım ve Danışmanlık](https://www.istechlabs.com).

📧 info@istechlabs.com
🌐 [www.istechlabs.com](https://www.istechlabs.com)
