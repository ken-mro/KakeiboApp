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
restarted after a change. The script reads the same file itself, so it also works when the user
runs it in their own terminal.

| Variable | What |
|---|---|
| `KAKEIBO_DRIVE_DIR` | the synced Drive folder, for example `G:\My Drive\Kakeibo` |
| `KAKEIBO_KEYSTORE` | the signing keystore, outside the repository (Visual Studio keeps its keys in `%LOCALAPPDATA%\Xamarin\Mono for Android\Keystore\<name>\<name>.keystore`) |
| `KAKEIBO_KEY_ALIAS` | the key's alias in it |
| `KAKEIBO_CERT_SHA256` | the SHA-256 of the certificate the installed app is signed with. Required: an APK signed with another key would not install over the app. `apksigner verify --print-certs <a released apk>` shows it. |
| `KAKEIBO_STORE_PASS`, `KAKEIBO_KEY_PASS` | the passwords. Optional: without them the script asks in a terminal. They reach the script only through Claude Code's environment; the script never reads them from the file itself, so in the user's own terminal it always asks. When set, MSBuild reads them itself through `env:`, so they never appear in a command or a log. |

Claude never asks for a password in chat, types one, prints one or puts one anywhere. When a
check needs to say whether a variable is set, it prints set/unset only.

## 1. Start

- Work from the latest `master`: `git checkout master && git -c http.sslBackend=schannel pull --ff-only`.
  Open PRs that should be in this build are merged first (`gh pr list --state open`); ask about any
  whose status is unclear.
- `Repository/Constants.cs` must hold the real Syncfusion key in the working tree (it does on this
  machine; never stage it). The script stops on the committed dummy key.
- **Version.** By default nothing changes: the build uses `ApplicationDisplayVersion` (versionName)
  and `ApplicationVersion` (versionCode) in `KakeiboApp.csproj` as they are. Look at what Drive
  already has (`ls "<KAKEIBO_DRIVE_DIR>"`): the APK is named like Visual Studio's archive,
  `com.companyname.kakeiboapp_<versionCode>.apk`. If that file is already there, ask the user
  whether to replace it (`--overwrite`) or raise the version. Raising it is a code change: a branch,
  both values in the csproj (versionCode up by 1), and `/ship-pr` when the user asks, then back to
  the merged `master`.
- Close Visual Studio's build of the project if it is building: it shares `obj` and can lock files.

## 2. Build, sign and copy to Drive

```bash
bash .claude/skills/android-release/scripts/build_apk.sh            # add --overwrite only when the user said so
```

It does a clean `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk` signed
with the keystore, checks that it is signed with the key in `KAKEIBO_CERT_SHA256`, copies the APK to `KAKEIBO_DRIVE_DIR` and checks the copy byte for byte. The last line is the path
on Drive.

- **`NEEDS_PASSWORD` (exit 3)**: the passwords are not in the settings and Claude's shell has no
  terminal to ask in. Do not ask for the password. Tell the user to run the same command in their
  own Git Bash in the repository folder, where it asks for it, and to paste the output (or say
  whether it ended with the Drive path). Alternatively, they can add `KAKEIBO_STORE_PASS` to
  `.claude/settings.local.json` themselves and restart Claude Code.
- **Other `FAIL` lines** say what is wrong. A build error is a code problem on `master`: fix it on a
  branch, not by changing the build settings. A key mismatch means the wrong keystore or alias:
  never sign with another key or change `KAKEIBO_CERT_SHA256` to get past it.

## 3. Check that Drive has it

The copy in the synced folder is not yet on Google Drive: Drive for desktop uploads it in the
background (a 40 MB APK takes a minute or so). The script warns if `GoogleDriveFS.exe` is not
running.

- If the Google Drive connector is available, search for the file name
  (`com.companyname.kakeiboapp_<versionCode>.apk`) and check that its modified time is the build's.
  It may take a few minutes to appear; look again once or twice before calling it missing.
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
