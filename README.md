# TotalMix Key Control

A lightweight Windows tray app that maps global hotkeys to a [RME TotalMix FX](https://www.rme-audio.de/totalmix-fx.html) fader via OSC. Control your interface volume with keyboard shortcuts, see a live on-screen display (OSD) with the exact dB value, and let the app update itself automatically.

## Installation

1. Download **`TotalMixKeyControl-win-Setup.exe`** from the [latest release](https://github.com/alakangas/TotalMixKeyControl/releases/latest).
2. Run the installer — it's a one-click install (no wizards, no admin rights required).
3. The app launches automatically after installation and starts on Windows boot by default.

The .NET 8 Desktop Runtime will be installed automatically if you don't already have it.

A **portable zip** is also available on the releases page if you prefer not to install.

## Getting Started

On first launch a Setup dialog appears. The defaults work out of the box if TotalMix FX is configured with:

- **Incoming OSC port:** 7001
- **Outgoing OSC port:** 9001

Set your preferred hotkeys for Volume Up, Volume Down, and Mute, then click OK.

You can reopen the Setup dialog at any time by double-clicking the tray icon or right-clicking it and choosing **Setup**.

## Features

- **Global hotkeys** for volume up, down, and mute.
- **On-screen display (OSD)** showing a translucent volume bar and the exact dB value reported by TotalMix.
- **Automatic updates** — the app checks for new versions in the background and offers a one-click restart to apply.
- **Run on Windows startup** — enabled by default, toggleable in Setup.
- **System tray app** — runs silently in the background with no taskbar clutter.

## Configuration

Settings are stored in `%AppData%\TotalMixKeyControl\config.ini` and can be edited via the Setup dialog:

| Section | Key | Description | Default |
|---------|-----|-------------|---------|
| OSC | IP | TotalMix FX host | `127.0.0.1` |
| OSC | Port | Incoming OSC port (send to TotalMix) | `7001` |
| OSC | OutPort | Outgoing OSC port (feedback from TotalMix) | `9001` |
| OSC | Address | Fader OSC address | `/1/volume4` |
| Volume | VolumeStep | Step size per keypress (multiplied by speed 1-4) | `0.01` |
| Hotkeys | VolumeUpHotkey | e.g. `Ctrl+Alt+Up` | — |
| Hotkeys | VolumeDownHotkey | e.g. `Ctrl+Alt+Down` | — |
| Hotkeys | VolumeMuteHotkey | e.g. `Ctrl+Alt+M` | — |
| OSD | Enabled | Show/hide the OSD | `1` |
| OSD | Position | `TopLeft`, `TopCenter`, `TopRight`, `BottomLeft`, `BottomCenter`, `BottomRight` | `BottomCenter` |
| OSD | MarginPreset | `None`, `Small`, `Medium`, `Large` | `Small` |
| OSD | DisplayTime | OSD display duration in ms | `1900` |

## Troubleshooting

- **No OSD / no volume change:** Make sure TotalMix FX has OSC enabled (Options > Settings > OSC) with matching port numbers.
- **Feedback values not showing:** Ensure the TotalMix outgoing port matches the OutPort setting, allow the app through your firewall, and confirm the OSC address points at a fader that emits feedback.
- **Hotkeys not working:** Some key combinations may be reserved by Windows or other software. Try a different combination.

## Building from Source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
dotnet build -c Release
```
