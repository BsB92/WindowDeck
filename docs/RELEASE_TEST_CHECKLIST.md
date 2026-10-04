# WindowDeck 1.3.0 local Windows acceptance checklist

Perform these checks on Windows 11 against the final published executable, outside Visual Studio.

1. Open PowerShell in the repository folder and run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Prepare-Release.ps1`.
2. Open `artifacts\WindowDeck-1.3.0-win-x64` and confirm that `WindowDeck.exe` is present.
3. Close every WindowDeck instance started from Visual Studio, then run that published `WindowDeck.exe` directly.
4. Confirm that it starts without asking you to install the .NET Runtime.
5. Start it a second time and confirm that only one instance remains.
6. Confirm that the tray icon and its Open, Settings, Help, About, Start with Windows, and Exit commands work.
7. Confirm that the global hotkey shows and hides the flyout.
8. Move the cursor to another screen, use the hotkey, and confirm that the flyout appears on the cursor's screen.
9. Search for part of a window title and confirm that the list filters correctly.
10. Turn grouping off and on and confirm that application grouping follows the setting.
11. Activate a selected window and confirm that the exact selected window is activated.
12. Minimize a selected window and confirm that the exact selected window is minimized.
13. Close a selected window and confirm that only the exact selected window receives its normal close request.
14. With multiple Microsoft Excel windows open, close one from WindowDeck and confirm that the other Excel windows remain open and that any Save / Don't Save / Cancel prompt works normally.
15. Select English and inspect the flyout, tray menu, Settings, Help, and About.
16. Select Polski and inspect the same UI.
17. Select System; on Polish Windows confirm Polish, and on a non-Polish Windows UI confirm English.
18. Restart WindowDeck after each language change and confirm that the selected option persists.
19. Open Help and confirm that its instructions match the implemented behavior.
20. Open About and confirm: WindowDeck; Version 1.3.0; `Created by ::BsB!::`; `Designed for Windows 11` / `Zaprojektowano dla Windows 11`; and open-source/MIT License wording. Confirm that there is no OK button.
21. Select System, Light, and Dark appearance modes and check the flyout, Settings, Help, and About in each mode.
22. Enable Start with Windows and confirm its checked state persists.
23. Enable Start minimized to tray, restart WindowDeck, and confirm that the flyout remains hidden while the tray icon is available.
24. If practical, sign out or restart Windows and confirm autostart launches WindowDeck into the tray without elevation.
25. Leave WindowDeck idle and use Task Manager to check that CPU usage is practically 0% and RAM remains reasonable (normally in the tens of megabytes).
26. Move the pointer through the window list, search, resize, change grouping, and repeatedly show/hide the flyout; look for flicker regressions.
27. Use **Tray > Exit** and confirm that the process and tray icon close cleanly.

28. In Settings > Screen numbering, confirm one row per connected screen and unique defaults 1–N. Identify screens, assign your preferred numbers, test move buttons and Presentation Mode, then restart and check persistence. Reset must not produce out-of-range numbers.
29. In Notepad++, open Find with Ctrl+F. With Show auxiliary windows enabled, confirm Find is under its owner, the subgroup arrow works, and search reveals the matching child. Toggle the option off/on and verify its saved value after restart.
30. Confirm fresh settings enable auxiliary windows by default and explicit previous disabled settings remain disabled. Do not delete personal settings to run this check; use a separate Windows test account if necessary.
31. Read the English and Polish Help, including Auxiliary windows. Process Designer/Process Simulate compatibility has not been separately verified; do not claim universal application compatibility.
32. Compute `Get-FileHash .\artifacts\WindowDeck-1.3.0-win-x64\WindowDeck.exe -Algorithm SHA256` and compare it with the neighboring checksum file and generated release notes. Upload that exact executable; do not rebuild after computing the release hash.

## Validation recorded during preparation

- User confirmed Screen numbering and the Notepad++ auxiliary-window test on Windows.
- Remaining interactive checks against the final 1.3.0 published executable require local Windows testing.
- Compilation and relationship regression checks are recorded in the release PR; compilation alone does not validate Windows runtime behavior.
