# WindowDeck

**WindowDeck** is a lightweight Windows utility for quickly finding and switching between open windows, especially when working with many applications, documents, and screens.

Designed for Windows 11.

> **About this project**
>
> I am not a software developer. WindowDeck started as a practical idea for solving a problem I had in everyday work.
>
> The application was designed, specified, tested, and iteratively refined by me, while the code was created with the help of **ChatGPT** and **OpenAI Codex**.

## Download

Download the latest version from:

**[GitHub Releases](https://github.com/BsB92/WindowDeck/releases/latest)**

WindowDeck is distributed as a **single, self-contained `WindowDeck.exe`**.

No installer is required and no separate .NET Runtime installation is needed.

### Windows SmartScreen

WindowDeck is currently distributed without a code-signing certificate.

Because of this, Windows SmartScreen may show an **"Unknown publisher"** warning when you run `WindowDeck.exe` for the first time.

This warning does not mean that Windows detected malware. It means the application is unsigned and does not yet have an established SmartScreen reputation.

If you downloaded WindowDeck from the official GitHub Releases page, you can verify the file source before running it.

### Verify the download

SHA-256 for `WindowDeck.exe` version 1.0.0:

```text
967af1660e0a1207586745472b93ce13f4dbb5a980059f1ed8741a76b0adeff4
```

You can verify the downloaded file in PowerShell:

```powershell
Get-FileHash .\WindowDeck.exe -Algorithm SHA256
```

The resulting hash should match the value above.

## Features

- Quickly find and switch between open windows
- Search open windows by title
- Optional grouping by application
- Screen indicators such as `[ 1 ]`, `[ 2 ]`, `[ 3 ]`, etc.
- Exact-window activation
- Minimize a specific window
- Send a normal close request to a specific window
- Configurable global hotkey
- Opens on the screen containing the mouse cursor
- System tray integration
- Start with Windows
- Start minimized to tray
- Application icons
- Optional minimized-window display
- System / Light / Dark appearance
- English and Polish interface
- Automatic language selection with the **System** option
- Built-in Help and About windows
- Single running application instance

WindowDeck uses event-driven window tracking and does not continuously poll for open windows.

## Quick start

1. Download `WindowDeck.exe` from the latest GitHub Release.
2. Run `WindowDeck.exe`.
3. WindowDeck starts in the **system tray**, so no main window may appear immediately.
4. Open WindowDeck by:
   - clicking the WindowDeck tray icon near the clock, or
   - using the default global shortcut **Win + `**
5. Search for a window or select one from the list.
6. Click a window to activate it.

The global shortcut can be changed in **Settings**.

WindowDeck continues running in the system tray when the panel is hidden.

To close WindowDeck completely, use:

**Tray icon → Exit**

## Window actions

Each WindowDeck row represents one specific Windows window.

- Click the window title to activate it.
- `—` minimizes that specific window.
- `×` sends a normal close request to that specific window.

WindowDeck does not force-kill applications.

If an application needs confirmation before closing, its normal **Save / Don't Save / Cancel** dialog remains in control.

## Multiple screens

WindowDeck shows the screen containing each window using indicators such as:

- `[ 1 ]`
- `[ 2 ]`
- `[ 3 ]`
- and so on.

When WindowDeck is opened using the global hotkey, it appears on the screen containing the mouse cursor.

## Languages

WindowDeck currently supports:

- **System**
- **English**
- **Polski**

With **System** selected:

- Polish Windows UI → Polish
- other Windows UI languages → English

The language can be changed in Settings without restarting WindowDeck.

## Appearance

Available appearance modes:

- **System**
- **Light**
- **Dark**

Language and appearance settings are independent.

## Privacy and network access

WindowDeck does not include:

- telemetry
- analytics
- user accounts
- cloud services
- automatic update checks
- automatic network communication

WindowDeck runs locally on your computer.

## System requirements

The published release is:

- designed for Windows 11
- Windows x64
- self-contained
- distributed as a single executable

A separate .NET Runtime installation is not required for the published release.

## Build from source

Requirements:

- .NET 10 SDK
- Windows desktop development workload
- Windows
- Visual Studio or the .NET CLI

Clone the repository and build:

```powershell
dotnet build WindowDeck.sln -c Release
```

You can also open `WindowDeck.sln` in Visual Studio and run the application with **F5**.

## Create a release build

The repository contains a publish profile for the Windows x64 release.

Run:

```powershell
dotnet publish WindowDeck.csproj -p:PublishProfile=WinX64
```

The output is created in:

```text
artifacts\WindowDeck-1.0.0-win-x64
```

The release configuration uses:

- Release configuration
- `win-x64`
- self-contained deployment
- single-file publishing
- trimming disabled
- NativeAOT disabled

## Project documentation

Detailed product behavior and technical requirements are documented in:

[`SPEC.md`](SPEC.md)

## License

WindowDeck is open source under the [MIT License](LICENSE).

Copyright (c) 2026 **::BsB!::**
