# WindowDeck 1.0.0

WindowDeck is a lightweight, event-driven Windows utility for finding the right open window when many documents have similar names. It is designed for Windows 11; this statement does not claim that Windows 10 is unsupported.

## Features

- English and Polish interfaces, plus a **System** option that selects Polish for a Polish Windows UI culture and English otherwise.
- Configurable global hotkey (default `Win + \``) and placement on the screen containing the cursor.
- Search and optional grouping by application.
- Activation, minimization, and normal close requests addressed to the exact selected window.
- System tray controls and a single running application instance.
- Settings for **Start with Windows**, minimized startup, list presentation, and **System / Light / Dark** appearance.
- Native Help and About windows.
- Event-driven window tracking with no continuous polling.
- No telemetry, analytics, account, cloud service, automatic update check, or automatic network traffic.

## Run the published application

1. Obtain the `WindowDeck-1.0.0-win-x64` release folder.
2. Run `WindowDeck.exe`. The self-contained release does not require a separate .NET Runtime installation.
3. Use the tray menu's **Exit** command when you want to stop WindowDeck completely.

The release has no installer. See [`docs/RELEASE_TEST_CHECKLIST.md`](docs/RELEASE_TEST_CHECKLIST.md) for final Windows acceptance checks.

## Build for development

Install the .NET 10 SDK and a Windows desktop development workload, then open `WindowDeck.sln` in a compatible Visual Studio version or run:

```powershell
dotnet build WindowDeck.sln -c Release
```

Run from Visual Studio with **F5**, or use `dotnet run --project WindowDeck.csproj` on Windows.

## Create the release publish

From the repository root, run:

```powershell
dotnet publish WindowDeck.csproj -p:PublishProfile=WinX64
```

The profile creates a Release, `win-x64`, self-contained, single-file publish in `artifacts\WindowDeck-1.0.0-win-x64`. Trimming and NativeAOT are disabled so localization resources and assembly metadata remain intact.

## Project documentation

[`SPEC.md`](SPEC.md) is the source of truth for implemented behavior and product requirements. No canonical GitHub repository URL is documented because this repository currently has no configured Git remote or other authoritative URL metadata.

## License

WindowDeck is open source under the [MIT License](LICENSE).

Copyright (c) 2026 ::BsB!::
