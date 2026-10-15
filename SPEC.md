# WindowDeck v1 product specification

## 1. Status and authority

This document is the single source of truth for WindowDeck product requirements and functional decisions. Development must be incremental and follow the stages in section 17. A task must implement only its requested stage; features must not be built speculatively.

WindowDeck v1.3.0 is being prepared for release. Its release distribution is a Windows x64, Release, self-contained, single-file publish that does not require a preinstalled .NET runtime. Release publication remains a separate manual step.

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
- Coalesce bursts of events within approximately 100–200 ms. Continuous event activity must not postpone window-list refreshes indefinitely.
- Never continuously poll all windows with a timer. Re-running `EnumWindows` is allowed only when genuinely needed to restore consistent state.
- Keep event callbacks safe and lightweight, and marshal work appropriately to the application/UI context.
- Idle CPU usage should be practically 0%.

## 6. Window filtering and titles

- Track normal, user-facing top-level windows, including minimized windows when the corresponding setting is enabled.
- Appropriately exclude WindowDeck itself, hidden helper/system windows, cloaked windows, and irrelevant tool windows. The opt-in auxiliary-window behavior in section 24 is the exception for visible titled dialogs and floating tools.
- Do not rely on one fragile heuristic where stable Windows APIs support a more robust decision.
- Store the original external window title separately from the display title used by WindowDeck.
- Never modify the actual title of another application's window.
- For known applications such as Excel and PowerPoint, a redundant application suffix may be removed from the display title only when it can be identified safely and unambiguously. For example, `176011_Calculation_Round2.xlsx - Excel` may display as `176011_Calculation_Round2.xlsx`.

## 7. Grouping and sorting

- The default order is application, then A–Z by displayed window/document name.
- Within an Excel group, for example, `176011_BOM_Round2.xlsx` precedes `176011_Calculation_Round2.xlsx`, which precedes `176011_Manpower_Round2.xlsx`.
- On every refresh, render the complete group and row order as a consistent list; cached/reused window rows must not appear above their application group or between unrelated groups while reflowing.
- Do not add unnecessary alternative sorting systems in v1.

## 8. Activation and panel focus behavior

- Selecting a window entry must restore it if minimized and activate it, using documented APIs such as `ShowWindow`, `ShowWindowAsync` when justified, and `SetForegroundWindow`.
- Activating a selected external window must **not** close WindowDeck. This enables sequential selection and comparison of several similar documents without repeatedly reopening the panel.
- Handle focus and deactivation carefully so activation initiated by a WindowDeck entry is not mistaken for an ordinary outside click that should hide the panel.

## 9. Monitor identity and placement

- Each window entry shows a small Screen badge such as `[ 1 ]`, `[ 2 ]`, or `[ 3 ]`.
- By default, assign consecutive WindowDeck Screen numbers `1..N` to the currently connected screens, ordered by GDI device number and then device name. GDI device suffixes can be sparse and must not be displayed as Windows Identify numbers. When the user configures custom Screen numbering, the configured WindowDeck number replaces the default number everywhere in the application while the underlying Windows display identity remains unchanged.
- Do not show monitor model names, aliases, resolutions, or extra monitor-information tooltips.
- Update an entry's monitor when its window moves to another display.
- Use documented APIs such as `MonitorFromWindow` and `GetMonitorInfo`.
- When invoked by the global shortcut, show WindowDeck on the monitor containing the mouse cursor at the moment the shortcut is pressed. Cursor-based monitor selection has priority, and the foreground-window monitor must not override it for this invocation path. If the cursor position or its monitor cannot be determined unexpectedly, fall back to the ordinary non-hotkey placement behavior so the application fails safely.

## 10. WindowDeck flyout panel

- The panel must behave like a Windows 11 flyout rather than an ordinary application window.
- It slides from the right side of the active monitor and occupies almost all available height, preferably ending above the taskbar.
- It is an overlay: it must not reserve work area, resize other windows, or behave as a dock/AppBar.
- The visible flyout stays in the topmost Z-order band when opened and after it activates an external window, without taking focus back from that window. Normal Windows Z-order rules for other topmost or privileged windows still apply.
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

### Tray icon history maintenance (WindowDeck 1.4)

- Settings provides a localized **Maintenance > Remove old WindowDeck taskbar entries** action for every user of a release executable; it must not depend on developer scripts or tooling.
- The action is **manual only**: list historical Windows notification-area records whose executable path resolves to a different WindowDeck.exe from the currently running one. Preserve all records for other executables and for the current executable path, even if they cannot be distinguished further.
- Show the exact full paths and number of eligible entries in a confirmation dialog before proceeding. Cancel makes no changes.
- Export every selected current-user registry record into a user-accessible local backup before removing any. If backup or revalidation fails, do not delete anything. Do not restart Explorer or change tray visibility preferences automatically.
- Explain that Windows may cache the personalization UI and that sign-out/sign-in may be needed. If Windows changes or uses an unknown registry layout, report that there are no safely recognized entries rather than broadening the deletion rule.
- Use built-in Windows/.NET functionality, no administrator privileges, external dependencies, automatic cleanup, or deletion of other apps' history.

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

When more than one display is connected, each window row shows localized, wrapping monitor buttons using the resolved WindowDeck Screen numbering. Selecting one moves that exact HWND to the target work area while preserving its restored size and relative position where possible and retaining minimized or maximized state. A single-display system shows no monitor buttons.

A fixed flyout command bar provides access to the existing Help and Settings windows.

Presentation Mode is an in-session feature for reserving one connected display for a selected allow-list of windows:
- Enabling Presentation Mode creates a special Presentation group above the normal application groups.
- Each normal window row gets a small add control. Adding a window moves its WindowDeck entry into the Presentation group without moving the external window itself.
- Presentation-group membership is tracked by exact HWND plus process ID and exists only for the current WindowDeck process.
- The Presentation group lets the user choose the reserved display using the resolved WindowDeck Screen numbering. Presentation-group windows may remain on any monitor; membership means they are allowed on the reserved display, not that they must stay there.
- Locking the Presentation group scans visible, non-minimized windows currently on the selected reserved display. If windows outside the Presentation group are present, WindowDeck shows a confirmation/configuration dialog before protection is activated.
- In that dialog, each conflicting window must either be added to the Presentation group or assigned another display. A Move all to action can assign one destination display to all conflicting windows. Cancel leaves protection inactive.
- After successful confirmation, the reserved display is protected. Visible windows outside the Presentation group that later appear on the reserved display are moved to a non-reserved fallback display. If WindowDeck cannot maintain protection, it disables the lock instead of pretending protection is active.
- Unlocking stops reservation enforcement without leaving Presentation Mode. Disabling Presentation Mode clears the in-session Presentation group and reservation state.
- Presentation Mode requires at least two active displays.

When grouping is enabled, right-clicking the window list provides localized Collapse all groups and Expand all groups actions. These actions reuse the existing in-memory collapse state and do not persist it across application restarts.

Individual row Minimize and Close controls have a subtle visible border in both light and dark appearance modes so that they read clearly as buttons without adding a heavy visual treatment.

## 23. Custom Screen numbering

WindowDeck allows the user to choose the Screen numbers used by the application without changing Windows display settings.

- The Settings window contains a **Screen numbering** section.
- The section lists the currently connected screens and lets the user assign unique WindowDeck numbers `1..N`.
- A screen-number assignment applies consistently everywhere WindowDeck shows or uses screen numbers, including window-row move buttons, the current-screen indication, Presentation Mode, and the Presentation conflict dialog.
- The Settings section includes **Identify screens**. Clicking it temporarily shows a large WindowDeck Screen number on each connected screen, similar to Windows display identification.
- The Settings section includes **Reset**, which restores the consecutive default WindowDeck numbering for the current connected-screen configuration.
- Custom numbering is stored locally in the existing JSON settings file.
- WindowDeck must distinguish physical screens using a stable display identity exposed by documented Windows display APIs; model names alone are insufficient because two connected screens may have the same model name.
- Numbering is remembered per connected-screen configuration so the same laptop can use different numbering at home and at work. Configuration switching is automatic and has no separate user-facing profile selector.
- A previously unseen connected-screen configuration starts with consecutive default WindowDeck numbering until the user changes it.
- If a saved screen can no longer be resolved safely, WindowDeck falls back to consecutive default WindowDeck numbering rather than applying an uncertain assignment.
- The feature must remain local and event-driven and must not add polling, telemetry, network access, third-party dependencies, or a general-purpose monitor-management system.
- User-facing terminology for this feature is **Screen**, not **Monitor**.

## 24. Optional auxiliary windows

- Settings > Window list provides a persisted Show auxiliary windows checkbox, on by default for new installs and settings files that omit the property. Explicitly saved false values remain respected.
- When enabled, include visible, titled top-level owned/dialog and tool windows across applications. Continue excluding WindowDeck, shell, cloaked, untitled, zero-sized auxiliary and no-activate auxiliary helper windows. Do not enumerate embedded/docked controls as separate windows.
- Resolve ownership through documented GW_OWNER relations, not process/name guessing. Follow at most 64 same-process owner links with cycle detection, including hidden/untitled intermediate owners; use the highest eligible visible owner as the subgroup root. Invalid chains or ownership that cannot be resolved safely leave the window standalone. Cross-process ownership is not grouped in this version.
- Keep the application group; its main window row has an independent arrow to expand/collapse its auxiliary subgroup. Children are indented and initially expanded. Main titles still activate the main window. Multiple main windows in one process retain separate subgroups.
- With application grouping off, preserve the same window subgroups in the ungrouped list. If a main owner is filtered out or placed in a different Presentation list, its auxiliary window remains a standalone visible row in its own list.
- Search retains eligible owners for matching children and temporarily expands relevant subgroups without changing remembered collapse state. Collapse state is in-memory, tied to HWND plus PID, pruned when a window disappears, and reset at exit. Existing Collapse all/Expand all group commands include window subgroups.
- Individual actions continue using exactly the selected HWND plus PID. No automatic propagation of Presentation membership or bulk actions is added; Windows/application-defined ownership behavior can still affect related windows.
- Reuse existing event-driven refresh. Switching the setting immediately refreshes enumeration. Update English and Polish Settings text and Help.

## 25. Release preparation

- The Windows x64 publish profile derives its output directory from the project Version property.
- `scripts/Prepare-Release.ps1` publishes the executable, calculates its SHA-256, and renders release notes from the versioned template. It does not tag, push, merge, or publish a GitHub release.
- Publish the executable and its checksum file as GitHub Release assets; the release notes must contain the checksum of that exact executable. Do not retain a previous executable checksum as the latest-release checksum in README.
- Test the final published executable outside Visual Studio using the current release checklist before publication.

## 25a. Foreground priority for newly opened windows (v1.4)

- The main WindowDeck panel and Saved window sets manager may be topmost while the user deliberately interacts with them, but **a newly opened and foreground-activated window belonging to another application must not appear behind them**.
- When an external window is newly detected and becomes the foreground window, Windows should control its normal stacking and focus. WindowDeck temporarily **removes TopMost from its own visible forms**; never set or leave permanent TopMost on another application's windows, and never force-activate background windows.
- On a deliberate return to WindowDeck (mouse, hotkey, tray), the flyout can reassert its own topmost layer. Foreground event handling must not create a focus/Z-order loop or reshow a hidden flyout.
- When Open saved set launches multiple files, release WindowDeck's and the Saved sets manager's topmost layer **before launching**. Do not immediately show a modal completion MessageBox that obscures the newly opened applications; instead display results within the Saved sets manager for later inspection. Missing-file paths and counts must remain available.
- Respect Presentation Mode reserved-monitor enforcement, but do not use it as a reason to keep an unrelated new foreground window behind WindowDeck on a nonreserved monitor.
- Only act on newly seen, user-facing foreground windows; avoid lowering WindowDeck merely because a background/system/helper window was created. Preserve other v1.4 grouping, event refresh, tray-cleanup, selection and localization behavior.

## 26. Saved window sets (planned for v1.4.0)

Allow a user to preserve and reopen a named selection of working windows after restarting Windows, without closing or disturbing windows already open at restore time.

### Capture user interface

- Add a clearly labeled Saved sets / Zestawy okien action to WindowDeck's main interface, reachable without entering application Settings.
- The Save current windows flow presents a scrollable list of currently tracked user-facing windows, with application grouping, recognizable application names, window titles and **one checkbox per window**.
- **Every available window is selected by default when the dialog opens.** Provide clear **Select all / Zaznacz wszystkie** and **Deselect all / Odznacz wszystkie** controls. The user may change individual checkboxes after using either bulk action.
- Provide an editable, user-chosen set name and save exactly the selected window entries; reject saving an empty selection with a localized explanation. Saving is an explicit user action, never automatic on exit.
- Multiple separate saved sets can coexist. The saved-set picker lists names and offers Open, Rename, Update and Delete actions. Overwriting or deleting an existing set requires explicit confirmation; cancel never changes the stored set.

### Persistence and reopening

- Save local, versioned JSON under the current user's WindowDeck application-data directory, separate from normal Settings, so sets survive closing WindowDeck and restarting Windows. No cloud services, elevated privileges or third-party dependencies.
- A window's runtime HWND, process ID or display title alone is not a stable resource identifier across sessions. Save a **real restorable target** (such as a validated document path or browser URL, where the application provides it) alongside the visible name, application identity and support status. Do **not** pretend a title is a file path or fabricate document targets.
- Reopening a set is **additive**: never close any currently open window, terminate an application, or replace the existing workspace. If a saved resource is already open, do not create a duplicate; it may optionally be activated or left untouched. This applies to repeated Open actions too.
- Restart only resource types that WindowDeck can resolve reliably and safely. Show unsupported/unresolved entries in the capture or restore summary with an honest explanation; do not silently launch an application in place of a requested document or execute arbitrary commands from a saved set.
- Preserve the set even if some resources have moved, been deleted, require authentication, or their applications are unavailable. When opening, report which resources were reopened, already open, skipped, or failed. A partial failure must not undo successfully opened resources or affect unrelated windows.
- The initial feature does not promise unsaved document changes, transient dialogs, exact application-internal state (e.g. an Excel selection), all browser tabs, or exact screen geometry and placement. Document the supported resource types after confirming actual Windows-specific restore behavior in local tests.
- Local JSON may contain sensitive document paths/URLs. Store it under the current user profile, without uploading or logging its contents, and do not automatically reopen any saved set on app startup.

### Additional document and program restoration requirements (v1.4)

- **Prioritize reopening the exact file**, not merely the application process, when a reliable document path has been identified. For Office document windows continue using the native window/document mapping when available. An unresolved application window may still be saved as program-only when its executable path is known.
- In the Save current windows dialog include **Choose file / Wskaż plik** for each window. A user may explicitly attach a local document to Notepad, PDF readers (including Foxit Reader) or other applications when WindowDeck cannot get a dependable path automatically. Preserve the captured source executable so the linked document can be passed as one separate, safely quoted argument; if that executable no longer exists, fall back to the user's Windows file association. Never infer file paths from window title text.
- Block executing script, shortcut and executable files through document linking. Program-only saved entries may start the **same validated executable**, once per restore action, but must not claim to recover tabs or application-specific state.
- Recommend (but never require) the application's option to open each document in a separate window for PDF readers and other tab-capable programs; it may improve window-level capture, but it does not itself guarantee a restorable full document path.
- For each missing or moved document, report its **full saved path** and continue opening the other set members. Never terminate or replace any currently open windows. Reopening is best-effort when another application instance cannot expose a document's exact path, and duplicate prevention for manually linked third-party documents is not guaranteed after the short launch-in-progress window.
- Open the Saved window sets manager **beside WindowDeck, on its left where screen space permits**, rather than behind the main flyout. It remains visible above the flyout; the capture dialog also remains accessible.

### Automatic document discovery and modal stacking (v1.4 follow-up)

- The Save current windows dialog must be modal, visible and usable **above** the TopMost Saved window sets manager, even when both are on the same screen and WindowDeck itself is TopMost. Place it within the same screen working area near its owner and activate it after showing.
- **Exact file restoration is the primary purpose of saved sets.** For document-focused programs (Notepad, Foxit, other PDF readers and application windows whose titles clearly contain a document filename), never treat reopening the blank program as a successful document restore.
- Capture exact document paths directly through supported application automation where available. For other document-focused windows, make a **best-effort, read-only** search in the current user's Windows Recent document links and, where present, Foxit PDF Reader's recent-file history. Accept only an existing local document with a complete filename clearly matching the window title and **exactly one unique matching path**. Do not guess among duplicate names, recursively scan disks, read other users' files, inspect other processes' handles or require administrator permissions.
- A clearly identified document lacking a trustworthy path is marked **Document path not found / Nie znaleziono ścieżki dokumentu**; it can be saved in a set but must be reported as unrestorable until the user supplies its actual file. Do not silently relaunch the application instead.
- **Choose file / Wskaż plik** is an optional fallback, not the default workflow. Explain the button in the save dialog. It assigns a real local document to a saved window when path discovery is unavailable or ambiguous. Browser windows may remain program-only, without tabs.
- Opening a set must continue past one missing document and report its saved full path. It must never silently substitute a blank program for a missing/unresolved document. Always keep current windows open.

### Saved-set capture status indicators (v1.4 UI refinement)

- Each saved-window row shows a compact **green check mark** when a supported file/folder exists and can be requested for reopening, or an app-only entry has a valid executable path. Draw the mark without an emoji, colored badge or square background.
- Show a visually matching **red X** for unsupported entries, unresolved document paths or targets currently unavailable. Do not imply that a file was successfully reopened; this is a save-time readiness check.
- **Choose file / Wskaż plik** is shown **only alongside a red X**, to repair an unresolved entry. Hide it for valid files/folders and for valid program-only items. When an exact file is chosen, update the row immediately to a green check and hide the button.
- Keep a visible **program only / bez kart** label on browser and other program-only entries, even with a green check, so the tick does not imply recovered tabs. Status meanings must be available through localized accessible names/tooltips and consistent across light/dark themes.
- Keep all save-selection checkboxes checked by default; bulk selection, scrolling, responsive row widths, and saved-set behavior must remain unchanged.

### Update set — saved membership marker (v1.4)

- In **Update set**, every entry **already belonging to the selected set** is **checked by default**, whether currently open or closed. Windows newly discovered during this update remain unchecked until explicitly added.
- Display a small **theme-aware floppy-disk icon**, drawn as a vector without emoji or colored square, **beside each previously saved entry**. Never show it for a new entry until its addition has actually been saved and the set is edited again.
- The floppy-disk icon conveys **membership**, not restore readiness. Keep the separate green check/red X for reopenability, even when an existing document path is no longer available. Include localized accessible text and tooltip for the disk.
- Unchecking a saved member requests its removal **only after confirmation and save**; cancel leaves original sets unchanged. The indicator remains a statement of existing membership throughout editing.
- Preserve independent select-all/deselect-all actions, the optional Choose file control for unrecognized documents, responsive list widths, and grouping by application. Update Help in Polish and English to distinguish all statuses clearly.

### Close set / Zamknij zestaw (v1.4)

- Add a separate **Close set / Zamknij zestaw** action beside Open set. Closing a set is **not deleting or updating** its definition; it remains saved for the next working session.
- Discover currently visible/minimized top-level application windows with the normal window enumerator; close **only** windows for which the current window's reliable full document/folder path exactly matches a path saved in the selected set. Matching must ignore case on Windows, normalize paths, and distinguish a file from a folder. Never match on title or process name alone.
- Do **not** close unsupported windows, entries stored as **program only** (browsers with tabs, etc.), or documents that WindowDeck cannot currently map to their actual window. These may be skipped and reported rather than risk closing unrelated tabs/documents. Never terminate a process or use a forced shutdown.
- Before any close request, show an explicit confirmation with the number of matched windows and their titles, explaining that programs may prompt to save unsaved changes. Cancel sends no close requests.
- After confirmation, revalidate the live HWND, process ID, and exact document/folder mapping before each close request; ignore disappearing/replaced/ambiguous windows.
- Request graceful closing using WindowDeck's existing `WindowActions.RequestClose` (native SC_CLOSE command). Excel, Word, Notepad, and other apps own the unsaved-change prompts; the user may choose Save, Don't Save or Cancel. **Do not bypass those prompts or claim that a posted request guarantees closure.**
- Temporarily relinquish the topmost layer of WindowDeck and Saved sets before requesting closes, so the target app's own confirmation dialogs can appear on top. Display the number of requests and skipped windows in the Saved sets manager **inline**, without a modal completion dialog that would hide application save prompts.
- Reopening the set later remains possible and uses the original, unchanged saved entries.

### Non-destructive updates and readable set contents (v1.4)

- Updating a named saved set must **not replace it with only the currently open windows**. Every previously saved entry stays in the update dialog, even when that document is closed, and is checked by default.
- Newly detected open windows that are not already in the set appear **unchecked**, so adding them is explicit. Removing a stored entry requires deliberately clearing its checkbox; confirm the number of additions and removals before saving. Cancel must leave the original set untouched.
- Duplicate detection should prefer exact file/folder paths; do not conflate different documents with identical titles. Updating a set with zero currently open windows must work.
- The update dialog includes the set name, which may be edited there. There is **no separate Rename button or Rename dialog**.
- Replace the dense table with a **borderless, grouped-by-application preview** in the Saved sets manager. Each collapsible application header shows the name and document count. Below it each saved window occupies one softly separated two-line card: full title (ellipsized visually when needed), compact file path, and matching green-check/red-X readiness indicator plus clear status text. Show the full, unchanged path in a tooltip and provide **Copy full path** from a right-click menu. Use vertical scrolling, no horizontal table scrolling, native theme colors, no white column headers or gridlines. Keep the left saved-set list and inline restore report.
- Keep restore summaries separate from the file list, rather than prepending multi-line status text to its contents.

### Acceptance scenarios

- Capture 10-20 windows with all checkboxes initially selected; deselect a few, save a named set and confirm it contains **only** selected entries.
- Exercise Select all and Deselect all after individual selections, and reject a fully deselected save.
- Save two different named sets, rename one, update another and delete one with confirmation.
- Restart Windows with saved applications closed; Open one set and confirm supported documents/folders are reopened.
- Open the set again with some resources already open; verify no duplicate resources and no currently open window is closed or replaced.
- Include moved/missing documents, unsupported windows and permission/access failures; verify honest reporting and unaffected existing windows.
- Verify both Polish and English UI, existing grouping, Presentation Mode, keyboard shortcut, notification icon and WindowDeck's lightweight idle behavior.

Implementation must be incremental on a dedicated branch and **requires local Windows 11 testing** before merging. In particular, a correct file/document reopening mechanism needs separate validation per supported application rather than inferring targets from generic window titles.
