# TripleDevBox

A single-window **WinUI 3** desktop app (Windows App SDK, unpackaged, **.NET 10**) that puts three everyday developer tools side by side in one window:

- **Unix Timestamp Converter** (left) — bidirectional and live as you type. Auto-detects seconds vs. milliseconds, shows UTC, local, ISO 8601, and a relative ("2 hours ago") view, plus a **Now** button.
- **GUID Generator** (right) — generates v4 GUIDs with **Uppercase / Braces / Hyphens** toggles (defaults produce `7076b156-8dfb-4092-b60a-8e6b1e7f900f`), a count field to make N at once, a live format preview, and copy.
- **JSON Escape / Unescape** (bottom) — escapes/unescapes JSON string content, mirroring the freeformatter.com behavior.

## Requirements

- Windows 10 version 2004 (build 19041) or later, x64
- .NET 10 SDK (to build)

## Build & run

```powershell
dotnet run --project TripleDevBox.csproj
```

Daily builds are framework-dependent (fast); the Windows App SDK is bundled so the app launches without a machine-installed runtime.

## Publish a portable build

Produces a **self-contained** folder that runs on any Windows 10/11 x64 machine with **no prerequisites** (the .NET runtime and Windows App SDK are both bundled), trimmed of unused Windows ML / DirectML / NPU components and non-English satellites:

```powershell
./publish.ps1          # -> dist/win-x64
./publish.ps1 -Zip     # also writes dist/TripleDevBox-win-x64.zip
```

Or use the Visual Studio **Portable-win-x64** publish profile. Copy the resulting `dist/win-x64` folder to any target machine and run `TripleDevBox.exe`.

## Project layout

| Path | Purpose |
|------|---------|
| `MainWindow.xaml` | Three-pane layout (timestamp + GUID on top, JSON below) |
| `Controls/TimestampConverter.*` | Unix timestamp tool |
| `Controls/GuidGenerator.*` | GUID tool |
| `Controls/JsonEscaper.*` | JSON escape/unescape tool |
| `Assets/AppIcon.ico` | Multi-resolution app icon (exe + taskbar) |
| `publish.ps1` | One-command portable publish |
