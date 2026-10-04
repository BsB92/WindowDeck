# WindowDeck 1.3.0 release guide

This guide prepares a release locally. It does not publish anything automatically.

## 1. Get the release source

In Visual Studio, stop debugging and open Developer PowerShell at the repository root:

```powershell
git fetch origin
git switch release/v1.3.0
git pull --ff-only
```

Do not discard local changes if Git reports a conflict. The tested release commit must ultimately be merged to master before tagging.

## 2. Publish and prepare assets

Exit any running WindowDeck through Tray > Exit. From the repository root run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Prepare-Release.ps1
```

The execution-policy override applies to that PowerShell process only. The script creates:

- `artifacts\WindowDeck-1.3.0-win-x64\WindowDeck.exe`
- `artifacts\WindowDeck-1.3.0-win-x64\WindowDeck.exe.sha256`
- `artifacts\WindowDeck-1.3.0-win-x64\RELEASE_NOTES.md` with the exact executable checksum filled in

Do not add generated assets to Git. The artifacts directory is ignored.

## 3. Test the published executable

Run `WindowDeck.exe` from that artifacts folder, outside Visual Studio. Follow [the release checklist](RELEASE_TEST_CHECKLIST.md). At minimum verify About says 1.3.0, tray/hotkey, screen numbering, a Notepad++ Find subgroup, settings persistence, English/Polish Help, and normal close confirmation.

Stop if publishing or acceptance fails. A successful compile is not a Windows runtime test.

## 4. Merge release preparation

After accepting the published executable, merge the release/v1.3.0 pull request into master. If no other changes were introduced, keep the already tested executable and checksum. If master contains additional functional changes, prepare and test a new executable from the final release source before tagging it.

## 5. Prepare the GitHub release draft

Open https://github.com/BsB92/WindowDeck/releases/new and set:

| Field | Value |
| --- | --- |
| Tag | `v1.3.0` (create new tag) |
| Target | `master`, containing the accepted release-preparation changes |
| Release title | `WindowDeck 1.3.0` |
| Description | Contents of generated `artifacts\WindowDeck-1.3.0-win-x64\RELEASE_NOTES.md` |
| Assets | The tested `WindowDeck.exe` and neighboring `WindowDeck.exe.sha256` |
| Pre-release | Off |
| Latest release | On, if shown |

Save a draft first. Wait for both asset uploads to finish. Confirm the description contains a real 64-character checksum, not the template placeholder. Do not paste the unrendered template from docs/notes.

## 6. Publish and verify

Click Publish release once the draft matches the accepted source and assets. Open the public release page, download the executable and checksum into a separate temporary folder, and compare `Get-FileHash` with the checksum in that release. Confirm the latest-release link points to 1.3.0.

The automatically attached Source code ZIP/tar.gz are source snapshots, not the runnable application.

To update your installed copy, exit the old WindowDeck, replace its executable with the new one, and run it from its permanent folder. Keep the existing local settings. If you move its permanent location, check the Start with Windows setting so startup does not keep pointing to the old executable.
