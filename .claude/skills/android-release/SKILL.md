---
name: android-release
description: Build KakeiboApp's signed Android APK (a clean Release build with the default settings, signed with the user's own keystore) and put it in the Google Drive folder that Drive for desktop syncs (My Drive/Kakeibo by default, set in .claude/settings.local.json), so the user can install it on their phone from Drive. Use whenever the user wants an installable build or to release a version ("APK を作って", "署名つきアーカイブを Drive に", "リリースして", "Drive にアップロード", "スマホに入れたい").
---

# Build the signed APK and put it on Drive

KakeiboApp is not on Google Play. A release is a signed `.apk` that the user downloads from Google
Drive on the phone and installs over the current app. Two things make that work:

- **The same signing key every time.** Android installs an update only when it is signed with the
  key the installed app has. With another key the only way in is uninstalling, which deletes all
  the household data. So the APK is checked against `KAKEIBO_CERT_SHA256` before it goes to Drive.
- **A versionCode that does not go down.** Android refuses a lower one. The same one installs as a
  reinstall.

`<scratch>` below means Claude's scratchpad directory. The flow runs in Git Bash. Network git
commands need `-c http.sslBackend=schannel`.

## One-time setup (the user's)

The settings go in the `env` of `.claude/settings.local.json`. That file is git-ignored,
`guard_secrets.py` blocks committing or pushing it, and Claude Code's Read and Write tools may not
touch it (`settings.json` denies them). Claude puts in the non-secret values only when the user asks;
the passwords are the user's to add. Claude Code reads it at start, so it is
restarted after a change. Paths are written with `/` (`G:/My Drive/Kakeibo`), so JSON needs no
`\\`. The script reads the same file itself, so it also works when the user
runs it in their own terminal.

| Variable | What |
|---|---|
| `KAKEIBO_DRIVE_DIR` | the synced Drive folder, for example `G:/My Drive/Kakeibo` |
| `KAKEIBO_KEYSTORE` | the signing keystore, outside the repository (Visual Studio keeps its keys in `C:/Users/<user>/AppData/Local/Xamarin/Mono for Android/Keystore/<name>/<name>.keystore`) |
| `KAKEIBO_KEY_ALIAS` | the key's alias in it |
| `KAKEIBO_CERT_SHA256` | the SHA-256 of the certificate the installed app is signed with. Required: an APK signed with another key would not install over the app. `apksigner verify --print-certs <a released apk>` shows it. |
| `KAKEIBO_STORE_PASS`, `KAKEIBO_KEY_PASS` | the passwords, put in by the user, so Claude can run the whole flow. `KAKEIBO_KEY_PASS` can be left out: it is then the same as `KAKEIBO_STORE_PASS`, as in the keystores Visual Studio makes. Claude Code passes them to the script as environment variables, and MSBuild reads them itself through `env:`, so they never appear in a command or a log. The script never reads them from the file itself: run in the user's own terminal, it asks for them instead. |

Claude never asks for a password in chat, types one, prints one or puts one anywhere. When a
check needs to say whether a variable is set, it prints set/unset only.

## 1. Start

- Work from the latest `master`: `git checkout master && git -c http.sslBackend=schannel pull --ff-only`.
  Open PRs that should be in this build are merged first (`gh pr list --state open`); ask about any
  whose status is unclear.
- `Repository/Constants.cs` must hold the real Syncfusion key in the working tree (it does on this
  machine; never stage it). The script stops on the committed dummy key.
- **Version.** The APK is named like Visual Studio's archive, `com.companyname.kakeiboapp_<versionCode>.apk`.
  Look at what Drive already has (`ls "$KAKEIBO_DRIVE_DIR"`) and compare it with `ApplicationVersion`
  (versionCode) and `ApplicationDisplayVersion` (versionName) in `KakeiboApp.csproj`.
  - Not on Drive yet: build as it is.
  - **Already on Drive: raise the version**, unless the user said to replace it (`--overwrite` in
    step 2) or gave a version. Do not ask; this is the user's standing choice.
    ```bash
    python -I .claude/skills/android-release/scripts/bump_version.py --dry-run   # versionCode +1, versionName 2.0 -> 2.1
    ```
    `--version X.Y` sets the versionName the user asked for; `--code-only` keeps it. A higher
    versionName shows 「使い方」 once after the update; say so in the report.
    Run it without `--dry-run` on a branch `release-<versionName>`, check that `git diff` shows
    only the two values, and commit just `KakeiboApp.csproj`: `Raise the version to <versionName> (<versionCode>)`.
    Asking for a release covers shipping this commit: run `/ship-pr` for the branch, then go back to
    the merged `master` (`git checkout master && git -c http.sslBackend=schannel pull --ff-only`).
- Close Visual Studio's build of the project if it is building: it shares `obj` and can lock files. The build deletes `obj/Release` and stops if it cannot.

## 2. Build, check the launch, then copy to Drive

2.5 went to Drive built over stale files in `obj` and crashed at launch on the phone (its theme
resources were missing), while the same code built clean was fine. So the build is always clean,
and the APK is launched on an emulator before it goes to Drive.

1. **Build** without copying:
   ```bash
   bash .claude/skills/android-release/scripts/build_apk.sh --no-copy
   ```
   It stops the build servers, deletes `obj/Release` and `bin/Release`, does a clean
   `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk` signed with the
   keystore, and checks that it is signed with the key in `KAKEIBO_CERT_SHA256`. The last line is the
   APK's path.
2. **Check the launch** on the emulator used for this project, **emulator-5556**, named on purpose:
   the user may have other emulators running with their own data (pixel_7 is theirs), and the check
   uninstalls the app. If emulator-5556 is not running, ask the user which emulator to use, or
   whether to skip the check; never pick one yourself.
   ```bash
   bash .claude/skills/android-release/scripts/launch_check.sh emulator-5556 <apk>
   ```
   It backs up the app's data (database and settings), installs the APK, launches it and checks after
   15 seconds that it is running with no crash in logcat. `FAIL` with the crash: do not copy it; find
   the cause, fix it on a branch, and build again. Afterwards, put the Debug build back on the
   emulator and restore the data with the printed backup:
   `bash .claude/skills/android-release/scripts/launch_check.sh emulator-5556 --restore <backup.tar>`.
3. **Copy** the checked APK to Drive:
   ```bash
   bash .claude/skills/android-release/scripts/build_apk.sh --copy-only   # add --overwrite only when the user said to replace it
   ```
   It checks the APK's versionCode and signature again, copies it to `KAKEIBO_DRIVE_DIR` and checks
   the copy byte for byte. The last line is the path on Drive.

- **`NEEDS_PASSWORD` (exit 3)**: the password is not in Claude Code's environment. Do not ask for
  it in chat. Tell the user to add `KAKEIBO_STORE_PASS` to the `env` of
  `.claude/settings.local.json` themselves and restart Claude Code (or, if Claude Code was not
  restarted since they added it, just to restart). Alternatively, they can run the same command in
  their own Git Bash in the repository folder, where it asks for the password, and paste the output.
- **Other `FAIL` lines** say what is wrong. A build error is a code problem on `master`: fix it on a
  branch, not by changing the build settings. A key mismatch means the wrong keystore or alias:
  never sign with another key or change `KAKEIBO_CERT_SHA256` to get past it.

## 3. Check that Drive has it

The copy in the synced folder is not yet on Google Drive: Drive for desktop uploads it in the
background (a 40 MB APK takes a minute or so). The script warns if `GoogleDriveFS.exe` is not
running.

- If the Google Drive connector is available, list the folder rather than search by title: a title
  search can come back empty for a file that is there. Find the folder's id from the parent of an
  APK already in it (`title contains 'kakeiboapp'`), then `search_files` with
  `parentId = '<that id>'`, and **follow `nextPageToken` to the end**: the connector may return
  one file per page, so the new APK is often on a later page. Check that its `fileSize` is the
  build's (`stat -c %s` on the copy) and its `createdTime` is the build's (UTC).
  The listing can lag more than 10 minutes behind the upload: wait about 10 minutes before the first
  look (in the background), and look again before calling it missing.
- Otherwise say that the file is in the synced folder and that the Drive for desktop icon in the
  taskbar shows when the upload is done.

## 4. Report to the user

- The version (versionName and versionCode), the size, the signer and its SHA-256, and the path on
  Drive.
- Whether Drive was seen to have it (step 3), or that the upload is still up to Drive for desktop.
- How to install it: open the file in the Drive app on the phone and install it. The first time,
  Android asks to allow installing apps from Drive. It installs over the current app and keeps the
  data, because the key is the same.
- If nothing was uploaded (a missing setting, the password step left to the user), say what is left
  to do.
