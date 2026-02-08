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
  <b>Verwalten Sie mehrere Entwicklungsprozesse über eine einzige Oberfläche.</b><br/>
  .NET 8 | WPF | Material Design 3
</p>

<p align="center">
  <a href="#funktionen">Funktionen</a> •
  <a href="#installation">Installation</a> •
  <a href="#verwendung">Verwendung</a> •
  <a href="#architektur">Architektur</a> •
  <a href="#konfiguration">Konfiguration</a> •
  <a href="#lizenz">Lizenz</a>
</p>

---

## Was ist das?

**DevManager** ist eine Windows-Desktopanwendung für Entwickler, die mehrere Microservices, APIs, Frontend-Anwendungen oder Hintergrunddienste ausführen. Organisieren Sie alle Ihre Prozesse nach Projekten und starten, stoppen, neustarten und überwachen Sie Live-Logs über ein einziges Panel.

---

<a id="funktionen"></a>
## Funktionen

### Prozessverwaltung
- **Stapelstart / -stopp** - Alle Prozesse oder projektbasiert mit einem Klick steuern
- **Graceful Shutdown** - Sauberes Herunterfahren über Ctrl+C-Signal, Force Kill bei Bedarf
- **Auto-Neustart** - Abgestürzte Prozesse automatisch mit konfigurierbaren Richtlinien neustarten
- **Orphan-Prozess-Erkennung** - Laufende Prozesse aus vorherigen Sitzungen automatisch erkennen und übernehmen

### Projektorganisation
- **Projektgruppen** - Prozesse nach Projekten organisieren (farbcodiert)
- **Auto-Projektscan** - Automatische Erkennung von .csproj- und package.json-Dateien
- **Framework-Erkennung** - Automatische Erkennung von React, Vue, Angular, Next.js, Nuxt usw.

### Überwachung
- **Live-Log-Streaming** - Echtzeit-stdout/stderr-Ausgabeüberwachung
- **Gesundheitsprüfungen** - Periodische Prüfungen über HTTP-Endpoint oder TCP-Port
- **Prozessstatus** - Sofortige Anzeige von PID, Laufzeit, Neustartanzahl
- **CPU/RAM-Metriken** - CPU-Auslastung und Speicherverbrauch pro Prozess

### Benutzeroberfläche
- **Material Design 3** - Modernes, dunkles Thema
- **Performante Log-Anzeige** - Virtualisierte Liste, gebündelte Aktualisierungen (100ms Batch)
- **Zirkulärer Log-Puffer** - Speicherschutz mit 5000-Zeilen-Limit pro Prozess
- **Mehrsprachig** - Türkisch, Englisch, Deutsch, Französisch

---

<a id="installation"></a>
## Installation

### Voraussetzungen
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Kompilieren und Ausführen

```bash
# Repository klonen
git clone https://github.com/user/DevManager.git
cd DevManager

# Kompilieren
dotnet build DevManager.sln

# Ausführen
dotnet run --project src/DevManager.App/DevManager.App.csproj
```

### Release Build

```bash
dotnet publish src/DevManager.App/DevManager.App.csproj -c Release -o ./publish
```

---

<a id="verwendung"></a>
## Verwendung

### Projekt hinzufügen

1. Klicken Sie auf **"+ Projekt hinzufügen"** unten links
2. Geben Sie den Projektordnerpfad ein und klicken Sie auf **"Scannen"**
3. Wählen Sie die gewünschten Prozesse aus den automatisch erkannten
4. Konfigurieren Sie Projektname, Farbe und Auto-Start-Einstellungen
5. Speichern Sie mit **"Projekt hinzufügen"**

### Prozesssteuerung

| Aktion | Beschreibung |
|--------|-------------|
| ▶ Play | Prozess starten |
| ■ Stop | Prozess stoppen (graceful → force) |
| ↻ Neustart | Prozess neustarten |
| 🗑 Löschen | Log-Anzeige leeren |
| 📋 Kopieren | Logs in Zwischenablage kopieren |

### Stapeloperationen
- **Obere Symbolleiste**: Alle Prozesse in allen Projekten starten/stoppen
- **Projektkopf**: Alle Prozesse im ausgewählten Projekt starten/stoppen/neustarten

---

<a id="architektur"></a>
## Architektur

```
DevManager.sln
├── src/
│   ├── DevManager.Core/           # Geschäftslogikschicht
│   │   ├── Models/                # Datenmodelle
│   │   │   ├── DevManagerConfig   # Hauptkonfiguration
│   │   │   ├── Project            # Projektdefinition
│   │   │   ├── ProcessDefinition  # Prozessdefinition
│   │   │   ├── ProcessInstance    # Laufzeitstatus
│   │   │   ├── HealthCheckConfig  # Gesundheitsprüfungseinstellungen
│   │   │   └── LogEntry           # Logeintrag
│   │   └── Services/              # Dienste
│   │       ├── ProcessManagerService    # Prozesslebenszyklus
│   │       ├── LogService               # Zirkulärer Log-Puffer
│   │       ├── ConfigurationService     # JSON-Konfiguration
│   │       ├── HealthCheckService       # HTTP/TCP-Gesundheitsprüfungen
│   │       └── ProjectScanner           # Auto-Projektscan
│   │
│   ├── DevManager.App/            # WPF UI-Schicht
│   │   ├── ViewModels/            # MVVM ViewModels
│   │   ├── Views/                 # XAML-Ansichten
│   │   └── Resources/             # Konverter, Stile, Lokalisierung
│   │
│   └── DevManager.Infrastructure/ # Plattformabhängige Schicht
│       ├── SystemTray              # Systembenachrichtigungsbereich
│       └── JsonConfigStore         # JSON-Speicherung
```

### Technologie-Stack

| Technologie | Verwendung |
|-------------|------------|
| .NET 8 (WPF) | Desktop-Anwendungsframework |
| MaterialDesignThemes 5.1 | Material Design 3 UI |
| CommunityToolkit.Mvvm 8.4 | MVVM-Infrastruktur |
| Microsoft.Extensions.DI | Dependency Injection |
| System.Management (WMI) | Orphan-Prozess-Erkennung |
| Hardcodet.NotifyIcon.Wpf | Systemtray (geplant) |

### Designentscheidungen

- **Graceful Shutdown**: Zuerst Ctrl+C über stdin, Force Kill bei Fehler
- **Zirkulärer Puffer**: Begrenzte Logs pro Prozess (Standard 5000 Zeilen) zur Speicherkontrolle
- **Gebündelte UI-Updates**: Log-Aktualisierungen alle 100ms zur Vermeidung von UI-Einfrieren
- **ConcurrentDictionary**: Thread-sichere Prozessverwaltung
- **Ereignisgesteuert**: Statusänderungen werden über Events an die UI kommuniziert
- **Atomare Dateischreibvorgänge**: Konfiguration wird in temporäre Datei geschrieben und dann verschoben

---

<a id="konfiguration"></a>
## Konfiguration

Die Konfigurationsdatei wird unter `%APPDATA%\DevManager\devmanager-config.json` gespeichert.

### Prozesseinstellungen

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

### Anwendungseinstellungen

| Einstellung | Standard | Beschreibung |
|-------------|----------|-------------|
| `maxLogLinesPerProcess` | 5000 | Maximale Logzeilen pro Prozess |
| `theme` | Dark | Thema (Dark / Light) |
| `language` | tr | Sprache (tr, en, de, fr) |
| `confirmBeforeStopAll` | true | Vor Stapelstopp bestätigen |
| `minimizeToTrayOnClose` | true | Beim Schließen in Tray minimieren |
| `startMinimized` | false | Minimiert starten |

---

## Roadmap

- [x] Prozessverwaltung (Start/Stopp/Neustart)
- [x] Projektorganisation
- [x] Live-Log-Überwachung
- [x] Auto-Neustart
- [x] Orphan-Prozess-Erkennung
- [x] Auto-Projektscan
- [x] CPU/RAM-Metriken (pro Prozess)
- [x] Mehrsprachige Unterstützung (TR/EN/DE/FR)
- [ ] Systemtray-Integration
- [ ] Einstellungs-UI
- [ ] Konfiguration Export/Import
- [ ] Einzelinstanz-Kontrolle

---

<a id="lizenz"></a>
## Lizenz

Dieses Projekt wird von [Istech Yazılım ve Danışmanlık](https://www.istechlabs.com) entwickelt.

📧 info@istechlabs.com
🌐 [www.istechlabs.com](https://www.istechlabs.com)
