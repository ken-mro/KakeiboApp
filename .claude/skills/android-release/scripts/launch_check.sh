#!/usr/bin/env bash
# Install a release APK on an emulator, launch it and check it keeps running.
#
#   bash .claude/skills/android-release/scripts/launch_check.sh <emulator serial> <apk>
#   bash .claude/skills/android-release/scripts/launch_check.sh <emulator serial> --restore <backup.tar>
#
# 2.5 went to Drive and crashed at launch on the phone; this catches that before the upload.
# The emulator is named on purpose, never picked: the user may have other emulators running, with their
# own data. Only an emulator is accepted, never a phone. The release APK is signed with another key than a
# Debug build, so the app is uninstalled first, which deletes its data. The data of a Debug build is backed
# up first (run-as works only on a debuggable build); the backup's path is printed, to put back with
# --restore once a Debug build is installed again.
#
# Last line: PASS, or FAIL with the crash.
set -uo pipefail
export MSYS_NO_PATHCONV=1
PACKAGE=com.companyname.kakeiboapp

SERIAL=${1:-}
case "$SERIAL" in
  emulator-[0-9]*) ;;
  *) echo "FAIL: name the emulator to use, for example emulator-5556 (adb devices lists them); a phone is never used"; exit 1 ;;
esac
adb devices | grep -q "^$SERIAL[[:space:]]*device$" || { echo "FAIL: $SERIAL is not connected"; exit 1; }
A="adb -s $SERIAL"

if [ "${2:-}" = --restore ]; then
  BACKUP=${3:?usage: launch_check.sh <serial> --restore <backup.tar>}
  $A shell run-as "$PACKAGE" true > /dev/null 2>&1 || { echo "FAIL: restore needs a Debug build installed on $SERIAL"; exit 1; }
  $A shell am force-stop "$PACKAGE"
  # adb push copies the bytes as they are (adb shell's stdin and stdout can turn LF into CRLF on Windows).
  # adb is a Windows program: give it the Windows form of a Git Bash path such as /tmp/....
  $A push "$(cygpath -w "$BACKUP" 2>/dev/null || echo "$BACKUP")" /data/local/tmp/kakeibo-data.tar > /dev/null && $A shell chmod 644 /data/local/tmp/kakeibo-data.tar \
    && $A shell "run-as $PACKAGE tar xf /data/local/tmp/kakeibo-data.tar" || { echo "FAIL: could not restore $BACKUP"; exit 1; }
  $A shell rm -f /data/local/tmp/kakeibo-data.tar
  echo "restored $BACKUP on $SERIAL"; exit 0
fi

APK=${2:?usage: launch_check.sh <serial> <apk>}
[ -f "$APK" ] || { echo "FAIL: $APK not found"; exit 1; }

if $A shell pm path "$PACKAGE" > /dev/null 2>&1 && $A shell run-as "$PACKAGE" true > /dev/null 2>&1; then
  BACKUP=$(mktemp -t kakeibo-emulator-data.XXXXXX).tar
  # exec-out, not shell: the tar is binary and must arrive byte for byte. The database and settings only:
  # Visual Studio's fast-deployment files come back with its next deploy.
  $A exec-out run-as "$PACKAGE" tar cf - files/KakeiboApp.db3 shared_prefs > "$BACKUP" 2>/dev/null
  tar tf "$(cygpath -w "$BACKUP" 2>/dev/null || echo "$BACKUP")" --force-local > /dev/null 2>&1 \
    || { echo "FAIL: the backup of $SERIAL's data is not readable, so the app was left installed: $BACKUP"; exit 1; }
  echo "emulator data backed up: $BACKUP"
fi

$A uninstall "$PACKAGE" > /dev/null 2>&1
timeout 300 $A install "$APK" > /dev/null || { echo "FAIL: the APK did not install on $SERIAL"; exit 1; }
$A logcat -c
$A shell monkey -p "$PACKAGE" -c android.intent.category.LAUNCHER 1 > /dev/null 2>&1
# The first launch of a release build takes a few seconds; a crash in the theme or the first page shows by then.
sleep 15

CRASH=$($A logcat -d -s AndroidRuntime:E | grep -A12 "Process: $PACKAGE" | head -14)
if [ -n "$CRASH" ] || ! $A shell pidof "$PACKAGE" > /dev/null; then
  echo "${CRASH:-the app is not running 15 seconds after launch}"
  echo "FAIL: $(basename "$APK") does not keep running on $SERIAL"; exit 1
fi
echo "PASS: $(basename "$APK") launched on $SERIAL and is running"
