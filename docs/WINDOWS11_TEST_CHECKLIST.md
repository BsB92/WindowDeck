# Stage 11 Windows 11 acceptance checklist

Run this checklist on Windows 11 after building the solution with zero errors.

## Icon and themes

- [ ] The executable, flyout/form, taskbar, and tray show the approved WindowDeck icon; the 16×16 tray image is clear, transparent, and unclipped.
- [ ] With Appearance set to System, WindowDeck follows both Windows Light and Dark app modes and responds to a Windows theme change without restarting.
- [ ] Forced Dark works while Windows is light; content, title bar, caption text, and caption buttons are dark-appropriate.
- [ ] Forced Light works while Windows is dark; content and native title bar remain light.
- [ ] Switching System, Light, and Dark in Settings previews Settings immediately and updates the flyout after OK without restarting.

## Flyout presentation and behavior

- [ ] Search has readable normal, placeholder, and focused states in both themes; filtering behavior is unchanged.
- [ ] Group headings are readable and subtle; grouping and sorting are unchanged.
- [ ] Rows are compact; icons and titles align; long titles ellipsize only when required; hover is subtle.
- [ ] Minimize hover is neutral and minimizes the exact HWND without activating it or hiding WindowDeck.
- [ ] Close hover is red with a white glyph and sends `WM_CLOSE` to the exact HWND; unsaved-document prompts still appear.
- [ ] The header is exactly `Screen`; badges retain spaces, for example `[ 1 ]`; screen numbering is unchanged.
- [ ] With application icons enabled, real external-app icons appear; when disabled, their space disappears and the title expands.
- [ ] Right-edge placement, full WorkArea height, left-edge-only horizontal resize, no vertical resize/free dragging, and TopMost behavior remain intact.

## Settings and lifecycle

- [ ] Settings is readable in Light and Dark, including its title bar, section labels, checkboxes, hotkey controls, Appearance list, and buttons; nothing is clipped.
- [ ] Tray Show/Hide, flyout X-to-hide, Esc-to-hide, Exit, restart, and single-instance behavior remain intact.
- [ ] The default and custom global hotkeys work and show the flyout on the Screen under the cursor.
- [ ] Start minimized to tray and Start with Windows work, and the saved startup state synchronizes with effective Windows startup state.
- [ ] Stage 5 window updates remain event-driven while the flyout is visible and hidden.

## DPI and resource sanity

- [ ] Repeat the visual checks at 100%, 125%, and 150% scaling; text/icons are not clipped, controls do not overlap, and action targets remain usable.
- [ ] Repeatedly show/hide WindowDeck, change themes, and open/close Settings; no crash, stale styling, or obvious resource degradation occurs.

## Window controls and bottom command bar

### Monitor buttons

- [ ] With one monitor, no monitor buttons are shown.
- [ ] With two monitors, buttons 1 and 2 are shown, the current monitor is visually active, and clicking the other button moves the exact selected window.
- [ ] With three or four monitors, all buttons remain on one row.
- [ ] With more than four monitors, buttons wrap after four, the window row grows, and controls do not overlap at 100%, 125%, or 150% DPI and at different flyout widths.
- [ ] A normal window keeps a sensible size and relative position and remains inside the destination work area.
- [ ] A maximized window moves to the destination and remains maximized.
- [ ] A minimized window moves without requiring the user to restore it first.
- [ ] Move normal, maximized, and minimized windows through Screen 1 → 2, 2 → 3, 3 → 4, and 4 → 1; after each click the selected monitor button becomes active.
- [ ] Repeat with a monitor left of the primary display, negative X/Y coordinates, different resolutions, different work areas, and mixed DPI scaling.

### Group header actions

- [ ] The arrow and application-name label collapse and expand the group.
- [ ] A collapsed group still displays Restore all, Minimize all, and Close all.
- [ ] After Minimize all, Restore all restores every minimized exact HWND while leaving already visible windows unchanged.
- [ ] Minimize all and Restore all leave a collapsed group collapsed.
- [ ] Minimize all affects every exact HWND in the group without changing collapse state.
- [ ] Close all always displays localized confirmation; Cancel closes nothing.
- [ ] Confirming Close all sends the normal close request to every exact HWND, including several independent Excel windows, without changing collapse state.
- [ ] Search temporarily exposes matching rows in collapsed groups and does not change remembered collapse state.

### Window-row regression

- [ ] With four monitors, every row shows its enabled application icon, ellipsized title, individual Minimize, individual Close, and buttons 1 / 2 / 3 / 4.
- [ ] Resize WindowDeck across its supported width range at 100%, 125%, and 150% DPI; the title consumes the remaining width and rows never become monitor-buttons-only.
- [ ] Recheck individual Minimize and Close, Minimize all, Close all with Cancel and confirmation, collapse/expand, and Search.

### Bottom bar, localization, and appearance

- [ ] Settings opens the existing Settings window and Help opens the existing Help window.
- [ ] Presentation Mode is visible, disabled, and described as coming soon; no presentation functionality is available.
- [ ] The command bar stays at the bottom while the list and flyout height change.
- [ ] Verify all new labels, confirmations, and tooltips with English, Polish, and System language.
- [ ] Verify the group headers, monitor buttons, confirmation, and bottom bar with Light, Dark, and System appearance.
