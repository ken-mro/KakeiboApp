---
name: android-release
description: Release KakeiboApp for Android - first check a clean Release build on the emulator for launch crashes and other fatal bugs, fixing and re-checking until none are left, then build the signed APK (default settings, the user's own keystore) and put it in the Google Drive folder that Drive for desktop syncs (My Drive/Kakeibo by default, set in .claude/settings.local.json). By default it also raises the version and ships it through a PR; in release-only mode it skips the PR and the version bump and just checks, builds and uploads. Use whenever the user wants an installable build or to release a version ("APK を作って", "署名つきアーカイブを Drive に", "リリースして", "リリースだけ作って", "Drive にアップロード", "スマホに入れたい").
---

# Release the Android app to Drive

KakeiboApp is not on Google Play. A release is a signed `.apk` that the user downloads from Google
Drive on the phone and installs over the current app. Three things make that work:

- **No fatal bug in the Release build.** 2.5 went to Drive built over stale files in `obj` and
  crashed at launch on the phone (its theme resources were missing), while every Debug check had
  passed. Only a Release build shows such problems, so **every release is checked on the emulator
  before it goes to Drive, and fixed until the check is clean** (stage 1), even a release with no
  code change. Builds are always clean (`obj/Release` deleted first).
- **The same signing key every time.** Android installs an update only when it is signed with the
  key the installed app has. With another key the only way in is uninstalling, which deletes all
  the household data. So the APK is checked against `KAKEIBO_CERT_SHA256` before it goes to Drive.
- **A versionCode that does not go down.** Android refuses a lower one. The same one installs as a
  reinstall.

`<scratch>` below means Claude's scratchpad directory. The flow runs in Git Bash. Network git
commands need `-c http.sslBackend=schannel`.

## Two modes

| | **Full release** (default) | **Release only** |
|---|---|---|
| When | 「リリースして」「APK を作って Drive に」 | the user asks for just the release: 「リリースだけ」「PR なしで Drive に」 |
| Stage 1, release check and fixes | yes | **yes**, the same |
| Version | raised when Drive already has it | **unchanged**; the APK on Drive with the same versionCode is replaced |
| Fixes from stage 1 | committed on the release branch and merged with the bump | committed on a branch, **not pushed** (no PR) |
| Built from | the merged `master` | the branch with the fixes, or `master` when there were none |

In both modes the APK that goes to Drive is the one that passed the check in stage 4.

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

**The emulator.** The release check uses **emulator-5556**, named on purpose. The user may have
other emulators running with their own data (pixel_7 is theirs), and the check uninstalls the app
(a release APK is signed with another key than a Debug build). If emulator-5556 is not running, ask
the user which emulator to use; never pick one yourself and never use a phone.

## 0. Start

- Pick the mode (table above). When it is unclear, it is the full release.
- Work from the latest `master`: `git checkout master && git -c http.sslBackend=schannel pull --ff-only`.
  Open PRs that should be in this build are merged first (`gh pr list --state open`), in the full
  release only; ask about any whose status is unclear.
- `Repository/Constants.cs` must hold the real Syncfusion key in the working tree (it does on this
  machine; never stage it). The script stops on the committed dummy key.
- Close Visual Studio's build of the project if it is building: it shares `obj` and can lock files.
  The build deletes `obj/Release` and stops if it cannot.
- **Version** (full release). The APK is named like Visual Studio's archive,
  `com.companyname.kakeiboapp_<versionCode>.apk`. Look at what Drive already has
  (`ls "$KAKEIBO_DRIVE_DIR"`) and compare it with `ApplicationVersion` (versionCode) and
  `ApplicationDisplayVersion` (versionName) in `KakeiboApp.csproj`. Already on Drive: the version is
  raised in stage 2, unless the user said to replace it or gave a version. Do not ask; this is the
  user's standing choice. Preview: `python -I .claude/skills/android-release/scripts/bump_version.py --dry-run`.
- Create the branch now, so fixes from stage 1 go on it: `release-<new versionName>` in the full
  release, `release-check-<versionName>` in release only.

## 1. Release check (repeat until clean)

1. Build the Release APK on the branch, without copying it to Drive:
   ```bash
   bash .claude/skills/android-release/scripts/build_apk.sh --no-copy
   ```
   It stops the build servers, deletes `obj/Release` and `bin/Release`, does a clean
   `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk` signed with the
   keystore, and checks the signature. The last line is the APK's path.
2. Check it on emulator-5556, with a new output folder for every run:
   ```bash
   bash .claude/skills/android-release/scripts/release_check.sh emulator-5556 <apk> <scratch>/release-check-<n>
   ```
   It backs up the app's data (database and settings), installs the APK, launches it, skips the
   first 使い方, opens the four tabs (生活費, 特別出費, 設定, ホーム), opens and closes the entry sheet,
   and checks after every step that the app is still running with no crash in logcat. A screenshot of
   each step and `logcat.txt` go to the folder. Exit 0 and `PASS`, or `FAIL` with the step and the
   crash.
3. **Look at every screenshot yourself.** A blank screen, an endless spinner, a page that did not
   load or a broken layout is a fatal bug even without a crash.
4. **Something fatal?** (crash, freeze, 「応答していません」, a screen that does not load, data loss.)
   Find the cause before changing code: the exception and stack in `logcat.txt`, the step that failed.
   A crash that a clean rebuild does not have is a build problem: fix the build (the scripts), not the
   app. Fix it on the branch, add a test that would have caught it when possible, and commit one
   concern per commit. Then **go back to 1 and run the whole check again with a new folder**: a fix
   can break something else, and only a full clean run counts.
5. When a run is clean, put the Debug build back on the emulator and its data with the backup the
   **first** run printed (later runs find the release build installed, which cannot be backed up, so
   they print none; keep that first path): uninstall the release APK, install a Debug build (for example `dotnet build -t:Install -f
   net10.0-android -c Debug "-p:AdbTarget=-s emulator-5556"`), then
   `bash .claude/skills/android-release/scripts/release_check.sh emulator-5556 --restore <backup.tar>`.

Non-fatal problems found on the way (a typo, a layout glitch) do not hold the release. List them for
the user, and fix them here only if they are small and the user would clearly want them in.

## 2. Version bump (full release)

When Drive already has the versionCode, on the same branch:
```bash
python -I .claude/skills/android-release/scripts/bump_version.py   # versionCode +1, versionName 2.0 -> 2.1
```
`--version X.Y` sets the versionName the user asked for; `--code-only` keeps it. A higher versionName
shows 「使い方」 once after the update; say so in the report. Check that `git diff` shows only the two
values and commit just `KakeiboApp.csproj`: `Raise the version to <versionName> (<versionCode>)`.

## 3. PR and merge (full release)

Asking for a release covers shipping this branch: run `/ship-pr` for it, with the bump and any fixes
from stage 1. In the PR body's testing section, say which release checks ran and their result, and the
fixes and re-checks if there were any. Then go back to the merged `master`
(`git checkout master && git -c http.sslBackend=schannel pull --ff-only`).

In release only, nothing is pushed: the fixes stay on the branch, and the report says so.

## 4. Build the archive, check it, copy it to Drive

The APK that goes to Drive is built again, on the merged `master` (full release) or the branch (release
only), and checked once more before it is copied: it is a new build.

1. `bash .claude/skills/android-release/scripts/build_apk.sh --no-copy`
2. `bash .claude/skills/android-release/scripts/release_check.sh emulator-5556 <apk> <scratch>/release-check-final`,
   then look at the screenshots and put the emulator back as in stage 1, step 5. `FAIL` here: back to
   stage 1 with the cause.
3. Copy the checked APK:
   ```bash
   bash .claude/skills/android-release/scripts/build_apk.sh --copy-only                            # full release
   bash .claude/skills/android-release/scripts/build_apk.sh --copy-only --any-branch [--overwrite] # release only
   ```
   It checks the APK's versionCode and signature again, copies it to `KAKEIBO_DRIVE_DIR` and checks
   the copy byte for byte. The last line is the path on Drive. `--overwrite` replaces an APK on Drive
   with the same versionCode: in release only that is the point; in the full release only when the
   user said to replace it. `--any-branch` is for release only, which copies a build that is not from
   `master`.

- **`NEEDS_PASSWORD` (exit 3)**: the password is not in Claude Code's environment. Do not ask for
  it in chat. Tell the user to add `KAKEIBO_STORE_PASS` to the `env` of
  `.claude/settings.local.json` themselves and restart Claude Code (or, if Claude Code was not
  restarted since they added it, just to restart). Alternatively, they can run the same command in
  their own Git Bash in the repository folder, where it asks for the password, and paste the output.
- **Other `FAIL` lines** say what is wrong. A build error is a code problem: fix it on a branch, not by
  changing the build settings. A key mismatch means the wrong keystore or alias: never sign with
  another key or change `KAKEIBO_CERT_SHA256` to get past it.

## 5. Check that Drive has it

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

## 6. Report to the user

- The mode, and the version (versionName and versionCode), the size, the signer and its SHA-256, and
  the path on Drive.
- The release check: the number of runs, what was fixed between them, and the final run's folder of
  screenshots. In release only, the branch the fixes are on, not pushed.
- Non-fatal problems left for later.
- Whether Drive was seen to have it (stage 5), or that the upload is still up to Drive for desktop.
- How to install it: open the file in the Drive app on the phone and install it. The first time,
  Android asks to allow installing apps from Drive. It installs over the current app and keeps the
  data, because the key is the same.
- If nothing was uploaded (a missing setting, the password step left to the user, a check that did
  not pass), say what is left to do.
