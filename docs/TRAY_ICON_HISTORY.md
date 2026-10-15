# Duplicate WindowDeck tray icon entries in Windows 11

## Cause and scope

Windows 11 may retain **historical notification icon entries** under
`HKEY_CURRENT_USER\Control Panel\NotifyIconSettings`. Running different copies of
`WindowDeck.exe` from Visual Studio output, versioned release artifact folders,
and a permanent application folder can therefore leave several entries in
**Settings > Personalization > Taskbar > Other system tray icons**.

This is different from having multiple **live** WindowDeck tray icons.
The application creates one WinForms `NotifyIcon`, disposes it on exit,
and uses a named mutex to prevent a second running instance.

Do not delete the full `NotifyIconSettings` registry key:
doing so resets the notification icon preferences for other applications.

## Prevent new stale entries

Copy the published `WindowDeck.exe` to a **single permanent path**
(for example, `%LocalAppData%\Programs\WindowDeck\WindowDeck.exe`), run
release builds from that location for normal use, and replace that file during
upgrades. Avoid launching successive versioned copies in `artifacts` for
everyday use. Visual Studio debug runs can still create temporary history
entries: Windows owns this history, not WindowDeck.

## Built-in cleanup for WindowDeck 1.4.0 and later

In **WindowDeck > Settings > Maintenance**, choose
**Remove old WindowDeck taskbar entries…** (Polish:
**Usuń stare wpisy WindowDeck z paska zadań…**).

1. The dialog lists the executable paths of Windows notification icon history
   entries pointing to **other** locations named WindowDeck.exe. Entries
   for the currently running EXE, and entries for other applications, remain
   untouched.
2. Review the paths and choose **Remove old entries**, or **Cancel** to do
   nothing. Before removal WindowDeck exports **all** selected registry keys
   to separate .reg backups under
   %LocalAppData%\WindowDeck\TrayIconBackups\<timestamp>\.
3. Close and reopen Windows Taskbar Settings. If it still displays old entries,
   sign out of Windows and back in, then inspect the list.

No administrator permission, separate script or developer tool is needed. This
button does **not** run automatically on application update.

**Limitations:** If Windows has multiple history entries for the **same path**
as the running EXE, the application intentionally preserves all of them rather
than guessing which entry is active. A debug build run from another directory
may consider a release EXE's history eligible, so carefully review the listed
paths. Registry data may differ between Windows versions; this behavior
requires testing on actual Windows 11 machines.

## Optional cleanup of WindowDeck entries only

The helper `scripts\Clear-WindowDeckTrayHistory.ps1` reads the current user's
registry and matches entries whose `ExecutablePath` ends with
`WindowDeck.exe`. It **never** touches entries for other executables.

1. Exit WindowDeck via **Tray > Exit** and close the Windows Taskbar Settings
   page. Do not just hide the WindowDeck flyout.
2. From the repository root, preview exactly which keys would be removed:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Clear-WindowDeckTrayHistory.ps1
   ```

3. Verify the listed `ExecutablePath` values really belong to WindowDeck.
   To back up and remove only those entries, run:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Clear-WindowDeckTrayHistory.ps1 -Apply
   ```

4. The script writes individual registry backups to
   `artifacts\tray-icon-backups\<timestamp>\` **before** removing anything.
   Restart Windows Explorer (or sign out and back in), then run the release
   executable from its permanent folder. Reconfigure the WindowDeck tray icon
   visibility preference if necessary.

### Limitations and safety

- This is a manual, opt-in repair, not an automatic startup action.
- Removing WindowDeck's icon history also resets **WindowDeck's own**
  tray icon visibility preferences.
- The exact Windows registry data format and Explorer caching may vary across
  Windows 11 builds. **Requires local Windows testing**.
- Restore an exported key if necessary with `reg import <backup-file.reg>`.
- If **multiple icons are simultaneously visible in the actual tray**, rather
  than merely listed in Settings, investigate that as a separate runtime bug.
