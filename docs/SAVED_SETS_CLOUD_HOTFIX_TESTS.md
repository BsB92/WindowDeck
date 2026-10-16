# WindowDeck saved sets cloud/Office hotfix — Windows validation

This branch is **not released** and must not be merged without approval and local Windows 11 testing. Building successfully is not proof that Office/SharePoint behaviors work.

## Repro and acceptance checklist

- [ ] Start from the stable v1.4.0 data file with existing named sets. Open **Saved sets** while at least one set contains cloud-synced paths. The dialog opens and reacts to clicks, drag, and closing while path status switches from "Checking…" to Ready/Unavailable.
- [ ] With an open Excel workbook in a locally synced OneDrive/SharePoint folder, click **Save current**. The main UI does not become **Not responding**; button displays **Scanning windows… / Odczyt okien…**. The selection form appears if scanning completes.
- [ ] While it scans, attempt a second Save current / Update set. No overlapping capture begins. Close the Saved sets manager during a slow scan: no crash or modal dialog should appear behind it.
- [ ] Run the same scenario with many Office and Explorer windows, slow placeholders and unavailable sync folders. If a 20-second scan/readiness limit is reached, the localized timeout is shown and the existing sets remain unchanged; the user can retry.
- [ ] For an unresolved open Excel document, click **Choose file… / Wskaż plik…**. Paste the complete local path from Explorer **Copy as path**, including quoted paths and paths with spaces. Save it while Excel is still open. The file is not opened, rewritten or closed by the path selector; the row becomes Ready if it exists.
- [ ] Test **Browse… / Przeglądaj…** for a normal local document and a synced locked workbook. If Windows refuses the workbook, the path text field fallback still works.
- [ ] Reject linking .lnk, .url, .exe, .bat, .ps1 and other unsafe extensions. A path that is offline/missing remains unavailable (red X), not falsely Ready.
- [ ] Save and reopen a set containing Excel, Word, PowerPoint, an Explorer folder and a linked PDF/TXT; confirm supported resources open correctly and missing items do not block the others.
- [ ] **Update set**: previously saved but closed documents remain checked with the floppy icon; newly opened documents begin unchecked. Cancel retains the saved JSON unchanged. Complete an update and confirm contents persist.
- [ ] Select sets repeatedly while status checks are running; no stale results appear on the newly selected set. Repeated opening/closing of the window does not throw a cross-thread or disposed-control exception.
- [ ] Verify both Polish and English, dark/light themes, dialog stacking and that standard window switching/Presentation Mode still work.
- [ ] Verify the resulting EXE built in Release configuration outside Visual Studio. Compare data in `%LocalAppData%\WindowDeck\window-sets.json` before/after a canceled or timed-out capture.

**Known boundary:** Cloud Office sessions may expose a remote URL instead of a local pathname through Office automation. The fallback requires a real local synced file path. Matching an already-open manually linked cloud document for duplicate prevention remains best-effort; do not claim to restore SharePoint browser tabs or infer a unique file from a title.
