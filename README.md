# WindowDeck

WindowDeck is a planned lightweight Windows 11 utility for quickly finding and activating the right open window, especially when many documents have similar names.

> **Project status: early development.** Stage 2 adds initial top-level window discovery and filtering. Window activation, the flyout panel, tray behavior, hotkeys, settings, and the other later-stage features are planned but are **not implemented yet**.

## What is available now

- A .NET 10 Windows Forms project and solution.
- Initial discovery of visible, user-facing top-level windows using documented Windows APIs.
- A temporary window list for validating the Stage 2 discovery results.
- The complete v1 requirements in [`SPEC.md`](SPEC.md).

## Planned direction

WindowDeck will eventually show current desktop windows in a lightweight Windows 11-inspired flyout, group and sort them for quick navigation, and activate the selected window. Development is deliberately incremental; planned behavior is documented in `SPEC.md`, which is the source of truth.

## Requirements

- Windows 11.
- Visual Studio 2026 Community, or another compatible environment that supports .NET 10 Windows desktop development.
- The .NET 10 SDK and the Visual Studio **.NET desktop development** workload.

## Open the project

1. Download or clone the repository.
2. Open `WindowDeck.sln` in Visual Studio.
3. If Visual Studio asks to install missing components, install the .NET 10 SDK and **.NET desktop development** workload, then reopen the solution.

## Build

In Visual Studio, select **Build > Build Solution**. A successful Stage 1 build should finish with 0 errors.

From a Developer PowerShell or terminal with the .NET 10 SDK installed, run:

```powershell
dotnet build WindowDeck.sln
```

## Run

In Visual Studio, press **F5** or select the green **Start** button. The temporary Stage 2 window lists the user-facing top-level windows found at startup. Use **Refresh** to repeat discovery manually. Closing the form ends the application. This window is only a validation surface and is not the final flyout.

## Development approach

The project is built in small, reviewable stages. Implemented functionality must not be inferred from the planned feature list; consult the current code and project status above. All product and functional decisions are governed by [`SPEC.md`](SPEC.md).
