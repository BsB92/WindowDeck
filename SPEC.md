# WindowDeck v1 product specification

## 1. Status and authority

This document is the single source of truth for WindowDeck product requirements and functional decisions. Development must be incremental and follow the stages in section 17. A task must implement only its requested stage; features must not be built speculatively.

WindowDeck v1.0.0 is release-ready. Its release distribution is a Windows x64, Release, self-contained, single-file publish that does not require a preinstalled .NET runtime. Release publication remains a separate manual step.

## 2. Product purpose and boundaries

WindowDeck is a lightweight Windows 11 utility for quickly finding and activating the correct currently open window. Its primary use case is navigating many similarly named documents in Excel, PowerPoint, File Explorer, Outlook, and other classic desktop applications.

The application will present open windows in a wide flyout-style panel from the right side of the active monitor. A user must be able to activate several windows in succession without WindowDeck closing after each selection.

WindowDeck must be fast, stable, event-driven, Windows-only, free, open source under the MIT License, and as portable as practical. It must require no account, cloud service, telemetry, analytics, network communication, or administrator privileges. It is not a Task Manager, PowerToys replacement, desktop manager, monitor manager, or general-purpose utility collection.

## 3. Required technology

- Language: C#.
- Framework: .NET 10 LTS.
- UI: Windows Forms (WinForms), using a Windows Forms App (.NET), not .NET Framework.
- Supported target: Windows 11. Prefer AnyCPU unless a required Win32 API makes that inappropriate.
- Prefer the .NET standard library, WinForms, P/Invoke, and stable documented Win32 APIs.
- Do not use WPF, WinUI 3, MAUI, Electron, Chromium, WebView, web frontends, or a local HTTP server as the UI layer.
- Do not add a NuGet package merely for convenience when .NET or Win32 can reasonably provide the capability.

## 4. Code and architecture conventions

- All source code, class and member names, variables, namespaces, comments, XML documentation, and UI text must be in English.
- Nullable reference types must remain enabled.
- Prefer straightforward, readable C#, small classes, clear responsibilities, composition, few abstraction layers, and code that is easy to debug.
- Avoid overengineering, speculative generic frameworks, service locators, elaborate event buses, and dependency injection without a demonstrated need.
- Do not implement future functionality before the stage that requires it.
- Isolate all P/Invoke declarations in a dedicated interoperability layer such as `Interop/NativeMethods.cs`; do not scatter `DllImport` or `LibraryImport` declarations through the application.
- Use stable, documented Win32 APIs. Do not use keyboard hooks, DLL injection, administrator privileges, or undocumented build-specific hacks when a stable alternative exists.

## 5. Window discovery and event-driven tracking

- At startup, perform one initial top-level window enumeration with `EnumWindows`.
- After the initial enumeration, normal updates must be driven by `SetWinEventHook` events, including window creation, destruction, title changes, show, hide, and location changes.
- Coalesce bursts of events with a debounce interval of approximately 100–200 ms.
- Never continuously poll all windows with a timer. Re-running `EnumWindows` is allowed only when genuinely needed to restore consistent state.
- Keep event callbacks safe and lightweight, and marshal work appropriately to the application/UI context.
- Idle CPU usage should be practically 0%.

## 6. Window filtering and titles

- Track normal, user-facing top-level windows, including minimized windows when the corresponding setting is enabled.
- Appropriately exclude WindowDeck itself, hidden helper/system windows, cloaked windows, and irrelevant tool windows.
- Do not rely on one fragile heuristic where stable Windows APIs support a more robust decision.
- Store the original external window title separately from the display title used by WindowDeck.
- Never modify the actual title of another application's window.
- For known applications such as Excel and PowerPoint, a redundant application suffix may be removed from the display title only when it can be identified safely and unambiguously. For example, `176011_Calculation_Round2.xlsx - Excel` may display as `176011_Calculation_Round2.xlsx`.

## 7. Grouping and sorting

- The default order is application, then A–Z by displayed window/document name.
- Within an Excel group, for example, `176011_BOM_Round2.xlsx` precedes `176011_Calculation_Round2.xlsx`, which precedes `176011_Manpower_Round2.xlsx`.
- Do not add unnecessary alternative sorting systems in v1.

## 8. Activation and panel focus behavior

- Selecting a window entry must restore it if minimized and activate it, using documented APIs such as `ShowWindow`, `ShowWindowAsync` when justified, and `SetForegroundWindow`.
- Activating a selected external window must **not** close WindowDeck. This enables sequential selection and comparison of several similar documents without repeatedly reopening the panel.
- Handle focus and deactivation carefully so activation initiated by a WindowDeck entry is not mistaken for an ordinary outside click that should hide the panel.

## 9. Monitor identity and placement

- Each window entry shows a small Screen badge such as `[ 1 ]`, `[ 2 ]`, or `[ 3 ]`.
- Prefer the Windows/GDI display identifier (for example, `\\.\DISPLAY1`) and derive the displayed number from it. Do not invent arbitrary numbering when Windows supplies an identifier.
- Do not show monitor model names, aliases, resolutions, or extra monitor-information tooltips.
- Update an entry's monitor when its window moves to another display.
- Use documented APIs such as `MonitorFromWindow` and `GetMonitorInfo`.
- When invoked by the global shortcut, show WindowDeck on the monitor containing the mouse cursor at the moment the shortcut is pressed. Cursor-based monitor selection has priority, and the foreground-window monitor must not override it for this invocation path. If the cursor position or its monitor cannot be determined unexpectedly, fall back to the ordinary non-hotkey placement behavior so the application fails safely.

## 10. WindowDeck flyout panel

- The panel must behave like a Windows 11 flyout rather than an ordinary application window.
- It slides from the right side of the active monitor and occupies almost all available height, preferably ending above the taskbar.
- It is an overlay: it must not reserve work area, resize other windows, or behave as a dock/AppBar.
- Initial width is approximately 600–750 px. The user can resize it by dragging its left edge, and the selected width is persisted.
- Full document names take priority. Use ellipsis only when text genuinely does not fit the available space.
- The panel is hidden by `Esc` or its `X` button. The `X` hides the application to the tray rather than terminating it.
- Visual motion must be minimal and lightweight.

## 11. Global hotkey

- The default global shortcut is **Win + `**.
- Use the appropriate OEM virtual-key code rather than treating the backtick only as a text character.
- Register the shortcut with `RegisterHotKey`; do not use a keyboard hook.
- The first shortcut press shows the panel and the next hides it.
- The user may change the shortcut. Before saving, attempt to register the proposed combination. If registration fails, show a simple message and do not persist the unusable combination.

## 12. Tray and application lifecycle

- WindowDeck runs continuously in the system tray using the standard WinForms `NotifyIcon`.
- Left-clicking the tray icon shows or hides WindowDeck.
- The tray context menu contains `Open WindowDeck`, `Settings`, `Help`, `About WindowDeck`, `Start with Windows` with its checked state, and `Exit`.
- Closing the panel with `X` only hides it to the tray. The process exits only through `Tray > Exit`.
- Permit only one running WindowDeck instance. Use a simple standard .NET/Windows mechanism; do not build elaborate IPC unless later requirements demonstrably need it.

## 13. Settings and persistence

- Store ordinary settings locally as JSON using `System.Text.Json`, preferably at `%LocalAppData%\WindowDeck\settings.json`. Do not use the registry for ordinary settings.
- The Settings UI contains:
  - **General:** `Start WindowDeck with Windows`; `Start minimized to tray`.
  - **Hotkey:** the configurable shortcut, defaulting to **Win + `**, with validation described in section 11.
  - **Window list:** `Group windows by application`; `Show application icons`; `Show screen number`; `Show minimized windows`.
  - **Appearance:** theme choices `System`, `Light`, and `Dark`.
- Settings loading must tolerate the absence of a settings file and should fail safely if local data is invalid.
- The Settings UI also contains **Language** choices `System`, `English`, and `Polski`; the default is `System`. This value is persisted in the existing `settings.json`, and a missing value in an older file means `System`. Language and Appearance are independent settings.
- Localization uses application-owned `.resx` resources and `ResourceManager`, with English base resources and Polish satellite resources. A central localization service owns the selected language, effective UI culture, supported-language definitions, and fallback to English. For `System`, a Windows UI culture beginning with `pl` selects Polish; every other current culture selects English. Adding a future language should primarily require its culture resource file, translations, and one entry in the central supported-language definition rather than form-specific culture logic.

## 14. Start with Windows

- `Start WindowDeck with Windows` is disabled by default.
- Use a simple per-user startup mechanism that needs no administrator privileges or installed service.
- Following Windows sign-in, WindowDeck starts in the background, does not automatically display the panel, creates its tray icon, and waits for the hotkey or tray interaction.

## 15. Appearance

- Aim for a native Windows 11, Fluent-inspired appearance with system light/dark behavior, standard Windows typography, subtle borders, light corner rounding, system/application icons, and minimal animation.
- Use reasonable WinForms and stable Windows API techniques rather than adding a heavy UI framework solely for appearance.
- Stability and low resource use take precedence over perfect WinUI imitation.

## 16. Performance, privacy, and dependencies

- Use one process, practically 0% idle CPU, and realistically target memory use in the tens of megabytes.
- Avoid continuous polling, network traffic, unnecessary background threads, and unnecessary timers. A debounce timer must never become a continuous window-scanning mechanism.
- Do not add telemetry, analytics, accounts, cloud access, or any other network communication.
- Keep dependencies minimal; prefer the platform and standard library.

## 17. Incremental implementation order

Implement in this order, with each stage being a separate small task or small set of logically related commits:

1. Project skeleton and successful build.
2. Window enumeration and filtering.
3. Window activation/restoration.
4. Monitor detection.
5. Automatic event-driven window updates.
6. WindowDeck flyout panel.
7. Search/grouping/sorting.
8. Tray icon and application lifecycle.
9. Global hotkey.
10. Settings and startup with Windows.
11. Windows 11 visual styling.
12. Final performance/stability cleanup.
13. Help, About, and release polish.

Before editing, read `AGENTS.md`, read the relevant parts of this specification, inspect the current code, and identify the smallest required change. During work, preserve working functionality, avoid unrelated refactors and large rewrites, build after meaningful changes, and correct introduced errors and warnings without merely suppressing them. After work, run a final build, inspect the diff, and ensure no build artifacts or unnecessary dependencies were added.

Stage 14 adds the English/Polish localization and language selection described in section 13. It also requires every row-level activate, minimize, and close action to use the exact `HWND` captured in that row without process-, application-, title-, or group-based target inference. A row close posts one normal system close command to that exact `HWND`; it must not kill or close a process, enumerate sibling windows, or bypass the target application's Save / Don't Save / Cancel lifecycle.

Stage 15 gives application groups user-facing names resolved from package and executable metadata, with a small alias fallback for known technical process names. A redundant application suffix is removed only from WindowDeck's separate display title when the final title segment exactly matches verified application metadata or an alias; the original Win32 title remains unchanged. When grouping is enabled, each group header can collapse or expand its rows. Collapse state is held only in memory for the current WindowDeck process, survives window-list refreshes and hiding to the tray, and resets to expanded when WindowDeck restarts. Active search temporarily shows matching rows from collapsed groups without changing their remembered in-session state.

## 18. Features excluded from v1

Do not add application ignore lists, monitor aliases, elaborate filters, profiles, synchronization, user accounts, plugins, cloud functionality, telemetry, analytics, network communication, automatic updates, heavy animations, extensive customization, desktop management, monitor management, or a PowerToys-like collection of features. Do not implement features "just in case."

## 19. Validation requirements

- Check the host operating system, installed .NET SDK, support for building `net10.0-windows`, and availability of Windows targeting packs/templates.
- On a non-Windows host, retain the Windows target. `EnableWindowsTargeting` may be used when technically necessary for cross-building; never convert the application to cross-platform just to make a cloud build easier.
- Report the exact result of every attempted build. Clearly distinguish a project failure from a missing SDK, targeting pack, or other environment limitation. Never claim runtime behavior was validated merely because it compiled.
- When it cannot be exercised in the current environment, label Windows-specific behavior **Requires local Windows testing**. This particularly includes tray behavior, global hotkeys, `SetForegroundWindow`, restoring minimized windows, multi-monitor placement, Windows startup, focus/deactivation behavior, Windows 11 visual integration, and real `SetWinEventHook` notifications.

## 20. Stage 1 scope

Stage 1 provides only a compiling WinForms solution, a minimal temporary startup form, repository documentation, and ignore rules. It must not implement `EnumWindows`, `SetWinEventHook`, external-window activation, monitor detection, tray behavior, global hotkeys, settings, Windows startup, or final styling.

## 21. Stage 13 scope

Stage 13 adds native, DPI-aware Help and About windows to the existing application lifecycle. Each window has at most one open instance, follows the selected System/Light/Dark appearance and native themed title bar, uses the WindowDeck icon, and performs no recurring background work. Help documents only implemented behavior. About displays the application version from assembly metadata and the visible authorship `Created by ::BsB!::`; source or license wording and external project links are included only when supported by repository metadata and license files. Stage 13 also permits narrow user-facing text and README corrections, but adds no window-management functionality, packaging, telemetry, analytics, automatic network communication, polling, or updater.

## 22. Post-v1 window controls and flyout command bar

Each grouped application header is a container with independent collapse, minimize-all, and close-all controls. Group actions operate on the exact currently displayed window handles; closing a group always requires localized confirmation. Collapse state remains process-memory-only, and search retains its temporary expansion behavior.

When more than one display is connected, each window row shows localized, wrapping monitor buttons using the existing Windows display numbering. Selecting one moves that exact HWND to the target work area while preserving its restored size and relative position where possible and retaining minimized or maximized state. A single-display system shows no monitor buttons.

A fixed flyout command bar provides access to the existing Help and Settings windows.

Presentation Mode is an in-session feature for reserving one connected display for a selected allow-list of windows:
- Enabling Presentation Mode creates a special Presentation group above the normal application groups.
- Each normal window row gets a small add control. Adding a window moves its WindowDeck entry into the Presentation group without moving the external window itself.
- Presentation-group membership is tracked by exact HWND plus process ID and exists only for the current WindowDeck process.
- The Presentation group lets the user choose the reserved display using the existing Windows display numbering. Presentation-group windows may remain on any monitor; membership means they are allowed on the reserved display, not that they must stay there.
- Locking the Presentation group scans visible, non-minimized windows currently on the selected reserved display. If windows outside the Presentation group are present, WindowDeck shows a confirmation/configuration dialog before protection is activated.
- In that dialog, each conflicting window must either be added to the Presentation group or assigned another display. A Move all to action can assign one destination display to all conflicting windows. Cancel leaves protection inactive.
- After successful confirmation, the reserved display is protected. Visible windows outside the Presentation group that later appear on the reserved display are moved to a non-reserved fallback display. If WindowDeck cannot maintain protection, it disables the lock instead of pretending protection is active.
- Unlocking stops reservation enforcement without leaving Presentation Mode. Disabling Presentation Mode clears the in-session Presentation group and reservation state.
- Presentation Mode requires at least two active displays.

When grouping is enabled, right-clicking the window list provides localized Collapse all groups and Expand all groups actions. These actions reuse the existing in-memory collapse state and do not persist it across application restarts.

Individual row Minimize and Close controls have a subtle visible border in both light and dark appearance modes so that they read clearly as buttons without adding a heavy visual treatment.
