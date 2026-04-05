# Valpero Tray

A lightweight Windows system tray application for [Valpero](https://valpero.com) — real-time uptime monitoring right in your taskbar.

![Windows 10/11](https://img.shields.io/badge/Windows-10%2F11-0078D4?logo=windows)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF-68217A)

---

## Features

- **Live status** — monitors, heartbeats, server agents updated on a configurable interval
- **Instant alerts** — tray icon turns red when any monitor is down
- **Open incidents** — see active incidents with duration at a glance
- **Server metrics** — CPU and RAM usage for connected server agents
- **Secure API key storage** — key is saved in Windows Credential Manager, never in a file
- **Zero clutter** — no main window, lives quietly in the system tray

---

## Requirements

- Windows 10 (1809 / build 17763) or later
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) — x64
- A [Valpero](https://valpero.com) account with an API key

---

## Getting Started

### 1. Install .NET 8 Runtime

Download and install the **.NET 8 Desktop Runtime (x64)** from:
https://dotnet.microsoft.com/en-us/download/dotnet/8.0

### 2. Download the latest release

Grab `Valpero.exe` (and accompanying files) from the [Releases](../../releases) page.

### 3. Run

Double-click `Valpero.exe`. A green dot will appear in the system tray (click the `^` arrow near the clock if it is hidden).

### 4. Enter your API key

Right-click the tray icon → **Settings**, paste your Valpero API key and click **Validate → Save**.

---

## Build from Source

```bash
git clone https://github.com/valpero/valpero-tray.git
cd valpero-tray/ValperoTray

dotnet publish -c Release -r win-x64 --self-contained false -o publish\
```

Output will be in the `publish\` folder.

---

## Project Structure

```
ValperoTray/
├── App.xaml / App.xaml.cs          # Application entry point, tray icon setup
├── Models/
│   └── Models.cs                   # API response models (Monitor, Incident, Agent, Heartbeat)
├── Services/
│   ├── ApiClient.cs                # HTTP client for Valpero REST API
│   ├── AppState.cs                 # Central state + auto-refresh timer
│   ├── CredentialManager.cs        # Windows Credential Manager P/Invoke wrapper
│   └── Settings.cs                 # User preferences (stored in %APPDATA%\Valpero)
└── Views/
    ├── PopupWindow.xaml(.cs)       # Main popup panel (click tray icon to open)
    └── SettingsWindow.xaml(.cs)    # API key + preferences window
```

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI framework | WPF (.NET 8) |
| Tray icon | Windows Forms NotifyIcon |
| API key storage | Windows Credential Manager (advapi32) |
| Settings | JSON in %APPDATA%\Valpero\settings.json |
| HTTP | HttpClient with SocketsHttpHandler |

---

## License

MIT © 2026 Valpero
