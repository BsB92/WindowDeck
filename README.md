# WindowDeck

WindowDeck is a lightweight Windows 11 utility for quickly finding and activating the right open window, especially when many documents have similar names.

> **Project status: release polish.** The v1 application features are implemented; packaging and release publication are not yet complete.

## What is available now

- A .NET 10 Windows Forms project and solution.
- Initial discovery of visible, user-facing top-level windows using documented Windows APIs.
- A right-aligned, topmost flyout that lists window titles and Screen numbers.
- Search, grouping by application, application icons, and `[ 1 ]`-style Screen indicators.
- Exact-window activation, including restoration of minimized windows.
- Exact-window minimize and normal close-request actions.
- Event-driven list updates when external windows change.
- A persistent system tray icon for showing, hiding, and exiting WindowDeck.
- A configurable global hotkey (default `Win + \``), including cursor-screen placement.
- Settings for Windows startup, minimized startup, list presentation, and System/Light/Dark appearance.
- Native Help and About windows.
- The complete v1 requirements in [`SPEC.md`](SPEC.md).

Development is deliberately incremental; implemented behavior and product requirements are documented in `SPEC.md`, which is the source of truth.

## Requirements

- Windows 11.
- Visual Studio 2026 Community, or another compatible environment that supports .NET 10 Windows desktop development.
- The .NET 10 SDK and the Visual Studio **.NET desktop development** workload.

## Open the project

1. Download or clone the repository.
2. Open `WindowDeck.sln` in Visual Studio.
3. If Visual Studio asks to install missing components, install the .NET 10 SDK and **.NET desktop development** workload, then reopen the solution.

## Build

In Visual Studio, select **Build > Build Solution**. A successful build should finish with 0 errors.

From a Developer PowerShell or terminal with the .NET 10 SDK installed, run:

```powershell
dotnet build WindowDeck.sln
```

## Run

In Visual Studio, press **F5** or select the green **Start** button. WindowDeck opens on the right side of the monitor containing the current foreground window and updates its list automatically. Click a row to activate that exact window. Press **Esc** or click **X** to hide the flyout, then left-click the WindowDeck tray icon to show it again. Right-click the tray icon and select **Exit** to close the application.

## Development approach

The project is built in small, reviewable stages. Implemented functionality must not be inferred from the planned feature list; consult the current code and project status above. All product and functional decisions are governed by [`SPEC.md`](SPEC.md).
