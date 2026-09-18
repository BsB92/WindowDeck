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
