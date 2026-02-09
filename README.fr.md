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
  <b>Gérez plusieurs processus de développement depuis une seule interface.</b><br/>
  .NET 8 | WPF | Material Design 3
</p>

<p align="center">
  <a href="#fonctionnalites">Fonctionnalités</a> •
  <a href="#installation">Installation</a> •
  <a href="#utilisation">Utilisation</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#licence">Licence</a>
</p>

---

## Qu'est-ce que c'est ?

**DevManager** est une application de bureau Windows conçue pour les développeurs exécutant plusieurs microservices, APIs, applications frontend ou services d'arrière-plan. Organisez tous vos processus par projet et démarrez, arrêtez, redémarrez et surveillez les logs en direct depuis un seul panneau.

---

<a id="fonctionnalites"></a>
## Fonctionnalités

### Gestion des Processus
- **Démarrage / Arrêt en Lot** - Contrôlez tous les processus ou par projet en un clic
- **Arrêt Gracieux** - Arrêt propre via signal Ctrl+C, force kill si nécessaire
- **Redémarrage Automatique** - Redémarrage automatique des processus plantés avec des politiques configurables
- **Détection de Processus Orphelins** - Détection et adoption automatiques des processus en cours des sessions précédentes

### Organisation des Projets
- **Groupes de Projets** - Organisez vos processus par projets (codes couleur)
- **Scan Auto de Projets** - Détection automatique des fichiers .csproj et package.json
- **Détection de Framework** - Reconnaissance automatique de React, Vue, Angular, Next.js, Nuxt, etc.

### Surveillance
- **Streaming de Logs en Direct** - Surveillance en temps réel des sorties stdout/stderr
- **Contrôles de Santé** - Vérifications périodiques via endpoint HTTP ou port TCP
- **Statut des Processus** - Affichage instantané du PID, temps de fonctionnement, nombre de redémarrages
- **Métriques CPU/RAM** - Affichage de l'utilisation CPU et de la consommation mémoire par processus

### Interface
- **Material Design 3** - Design moderne avec thème sombre
- **Affichage Performant des Logs** - Liste virtualisée, mises à jour groupées (batch 100ms)
- **Tampon de Logs Circulaire** - Protection mémoire avec limite de 5000 lignes par processus
- **Multi-Langue** - Turc, Anglais, Allemand, Français

---

<a id="installation"></a>
## Installation

### Télécharger (Recommandé)

> **Aucune installation .NET requise** — Self-contained, fichier unique.

| Plateforme | Télécharger |
|------------|-------------|
| Windows x64 | [DevManager-v1.2.0-win-x64.zip](https://github.com/uygaruludag/DevManager/releases/download/v1.2.0/DevManager-v1.2.0-win-x64.zip) |

1. Téléchargez le fichier ZIP et extrayez-le dans un dossier
2. Exécutez `DevManager.App.exe`
3. C'est tout !

### Compiler depuis les Sources

#### Prérequis
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
# Cloner le dépôt
git clone https://github.com/uygaruludag/DevManager.git
cd DevManager

# Compiler
dotnet build DevManager.sln

# Exécuter
dotnet run --project src/DevManager.App/DevManager.App.csproj
```

### Self-Contained Publish

```bash
dotnet publish src/DevManager.App/DevManager.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

<a id="utilisation"></a>
## Utilisation

### Ajouter un Projet

1. Cliquez sur le bouton **"+ Ajouter un Projet"** en bas à gauche
2. Entrez le chemin du dossier du projet et cliquez sur **"Scanner"**
3. Sélectionnez les processus souhaités parmi ceux détectés automatiquement
4. Configurez le nom du projet, la couleur et les paramètres de démarrage automatique
5. Enregistrez avec **"Ajouter le Projet"**

### Contrôle des Processus

| Action | Description |
|--------|-------------|
| ▶ Play | Démarrer le processus |
| ■ Stop | Arrêter le processus (gracieux → forcé) |
| ↻ Redémarrer | Redémarrer le processus |
| 🗑 Effacer | Effacer l'affichage des logs |
| 📋 Copier | Copier les logs dans le presse-papiers |

### Opérations en Lot
- **Barre d'outils supérieure** : Démarrer/arrêter tous les processus de tous les projets
- **En-tête de projet** : Démarrer/arrêter/redémarrer tous les processus du projet sélectionné

---

<a id="architecture"></a>
## Architecture

```
DevManager.sln
├── src/
│   ├── DevManager.Core/           # Couche logique métier
│   │   ├── Models/                # Modèles de données
│   │   │   ├── DevManagerConfig   # Configuration principale
│   │   │   ├── Project            # Définition de projet
│   │   │   ├── ProcessDefinition  # Définition de processus
│   │   │   ├── ProcessInstance    # État d'exécution
│   │   │   ├── HealthCheckConfig  # Paramètres de contrôle de santé
│   │   │   └── LogEntry           # Entrée de log
│   │   └── Services/              # Services
│   │       ├── ProcessManagerService    # Cycle de vie des processus
│   │       ├── LogService               # Tampon de log circulaire
│   │       ├── ConfigurationService     # Configuration JSON
│   │       ├── HealthCheckService       # Contrôles de santé HTTP/TCP
│   │       └── ProjectScanner           # Scan auto de projets
│   │
│   ├── DevManager.App/            # Couche UI WPF
│   │   ├── ViewModels/            # ViewModels MVVM
│   │   ├── Views/                 # Vues XAML
│   │   └── Resources/             # Convertisseurs, styles, localisation
│   │
│   └── DevManager.Infrastructure/ # Couche dépendante de la plateforme
│       ├── SystemTray              # Zone de notification système
│       └── JsonConfigStore         # Stockage JSON
```

### Stack Technologique

| Technologie | Utilisation |
|-------------|------------|
| .NET 8 (WPF) | Framework d'application bureau |
| MaterialDesignThemes 5.1 | UI Material Design 3 |
| CommunityToolkit.Mvvm 8.4 | Infrastructure MVVM |
| Microsoft.Extensions.DI | Injection de dépendances |
| System.Management (WMI) | Détection de processus orphelins |
| Hardcodet.NotifyIcon.Wpf | Zone de notification (planifié) |

### Décisions de Conception

- **Arrêt Gracieux** : D'abord Ctrl+C via stdin, force kill en cas d'échec
- **Tampon Circulaire** : Logs limités par processus (par défaut 5000 lignes) pour le contrôle mémoire
- **Mises à Jour UI Groupées** : Mises à jour des logs toutes les 100ms pour éviter le gel de l'UI
- **ConcurrentDictionary** : Gestion thread-safe des processus
- **Événementiel** : Les changements d'état sont communiqués à l'UI via des événements
- **Écritures Fichier Atomiques** : Configuration écrite dans un fichier temporaire puis déplacée

---

<a id="configuration"></a>
## Configuration

Le fichier de configuration est stocké dans `%APPDATA%\DevManager\devmanager-config.json`.

### Paramètres de Processus

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

### Paramètres de l'Application

| Paramètre | Par Défaut | Description |
|-----------|-----------|-------------|
| `maxLogLinesPerProcess` | 5000 | Lignes de log maximum par processus |
| `theme` | Dark | Thème (Dark / Light) |
| `language` | tr | Langue (tr, en, de, fr) |
| `confirmBeforeStopAll` | true | Confirmer avant l'arrêt en lot |
| `minimizeToTrayOnClose` | true | Minimiser dans la zone de notification |
| `startMinimized` | false | Démarrer minimisé |

---

## Feuille de Route

- [x] Gestion des processus (démarrer/arrêter/redémarrer)
- [x] Organisation des projets
- [x] Surveillance des logs en direct
- [x] Redémarrage automatique
- [x] Détection de processus orphelins
- [x] Scan automatique de projets
- [x] Métriques CPU/RAM (par processus)
- [x] Support multi-langue (TR/EN/DE/FR)
- [ ] Intégration zone de notification
- [ ] Interface des paramètres
- [ ] Export/Import de configuration
- [ ] Contrôle d'instance unique

---

<a id="licence"></a>
## Licence

Ce projet est développé par [Istech Yazılım ve Danışmanlık](https://www.istechlabs.com).

📧 info@istechlabs.com
🌐 [www.istechlabs.com](https://www.istechlabs.com)
