# LogiForUnity

Logitech Loupedeck / Logi Plugin Service plugin for controlling the **Unity Editor** — hotkeys, direct editor-API commands, and dials for continuous values.

## Features

- **Hotkey commands** — send Unity keyboard shortcuts (tools, play mode, windows, edit).
- **Bridge commands** — drive the editor directly via a companion package (`EditorApplication`, `Tools`, `SceneView`, `Undo`, …), independent of the user's Shortcut Manager settings or Unity version.
- **Dials** — adjust selected-object Transform (move/rotate/scale), scene zoom, and time scale, with the current value shown next to the dial.
- **Auto-installing companion** — the plugin carries an embedded UPM package (`com.logi.unity-bridge`) and, with the user's consent, installs it into the open Unity project's `Packages/` folder. Editor-only; never included in player builds.
- **Vector icons** — button artwork drawn in code, crisp at every size.
- **Korean localization** (`ko-KR`).

## How it works

The plugin runs a loopback TCP server; a companion script inside the Unity project connects as a client and survives domain reloads by reconnecting. Commands are dispatched on the editor main thread. When the bridge is not connected, bridge commands do nothing (no silent wrong-key fallback).

## Building

Requires the .NET SDK and the Logi Plugin Service (provides `PluginApi.dll`).

```powershell
dotnet build src/LogiForUnityPlugin.csproj -c Release
logiplugintool pack ./bin/Release/ ./LogiForUnity_1_0.lplug4
logiplugintool verify ./LogiForUnity_1_0.lplug4
```

## Installing

Double-click `LogiForUnity_1_0.lplug4`, or:

```powershell
logiplugintool install ./LogiForUnity_1_0.lplug4
```

After Unity starts, press the **Install Bridge** command in Logi Options+ to enable direct editor control. Until then, commands fall back to keyboard shortcuts.

## Platform

Windows only. Project detection uses WMI; macOS is not yet supported.

## License

MIT
