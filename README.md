# TotalMixKeyControl

A lightweight Windows app that maps global hotkeys to a TotalMix fader via OSC. It sends volume changes over UDP, listens for OSC feedback to reflect the exact fader value, and shows a simple on-screen display (OSD). Configuration lives in a small `config.ini`, with a first‑run Setup dialog to get you started quickly.

## Features
- INI‑compatible configuration (`config.ini`):
	- [OSC] IP, Port (incoming to TotalMix, default 7001), OutPort (feedback from TotalMix, default 9001), Address
	- [Volume] VolumeStep, MaxValue
	- [Hotkeys] VolumeUpHotkey, VolumeDownHotkey, VolumeMuteHotkey
	- [OSD] Enabled, DisplayTime, Position, MarginPreset
- Global hotkeys.
- OSC sender with proper string padding and big-endian floats.
- OSC feedback listener on the outgoing port to display the exact fader value reported by TotalMix.
- On-screen Display (OSD) with translucent green bar and dB readout driven by TotalMix feedback.

## Build
Requires .NET 8 SDK.

```
dotnet build -c Release
```

## Run
By default the app looks for `config.ini` next to the executable. You can also pass an explicit path as the first argument.

```
./TotalMixKeyControl.exe [optional-path-to-config.ini]
```

## Notes
- If you already have TotalMix OSC configured with Incoming=7001 and Outgoing=9001 (defaults), no extra setup is required. The app sends to `Port` and listens on `OutPort`.
- The OSD shows feedback values from TotalMix.
- If feedback doesn’t appear, ensure the TotalMix Outgoing port matches `OutPort`, allow the app in your firewall, and confirm your `Address` points at a fader that emits feedback.
