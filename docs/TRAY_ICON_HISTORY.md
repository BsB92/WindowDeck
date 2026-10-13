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
