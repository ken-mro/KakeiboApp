#!/usr/bin/env bash
# Check a release APK for fatal bugs on an emulator: install it, launch it, open the four tabs and the entry
# sheet, and look for a crash after each step. A screenshot of every step goes to <out dir>, to look at
# too: a blank screen or an endless spinner is a fatal bug even without a crash.
#
#   bash .claude/skills/android-release/scripts/release_check.sh <emulator serial> <apk> <out dir>
#   bash .claude/skills/android-release/scripts/release_check.sh <emulator serial> --restore <backup.tar>
#
# 2.5 went to Drive and crashed at launch on the phone; this catches that before the upload.
# The emulator is named on purpose, never picked: the user may have other emulators running, with their
# own data. Only an emulator is accepted, never a phone. The release APK is signed with another key than a
# Debug build, so the app is uninstalled first, which deletes its data. The data of a Debug build is backed
# up first (run-as works only on a debuggable build); the backup's path is printed, to put back with
# --restore once a Debug build is installed again.
#
# Last line: PASS, or FAIL with the step and the crash. <out dir> also gets logcat.txt.
set -uo pipefail
export MSYS_NO_PATHCONV=1
PACKAGE=com.companyname.kakeiboapp
HERE=$(cd "$(dirname "$0")" && pwd)
win() { cygpath -w "$1" 2>/dev/null || echo "$1"; }

SERIAL=${1:-}
case "$SERIAL" in
  emulator-[0-9]*) ;;
  *) echo "FAIL: name the emulator to use, for example emulator-5556 (adb devices lists them); a phone is never used"; exit 1 ;;
esac
adb devices | grep -q "^$SERIAL[[:space:]]*device$" || { echo "FAIL: $SERIAL is not connected"; exit 1; }
A="adb -s $SERIAL"

if [ "${2:-}" = --restore ]; then
  BACKUP=${3:?usage: release_check.sh <serial> --restore <backup.tar>}
  $A shell run-as "$PACKAGE" true > /dev/null 2>&1 || { echo "FAIL: restore needs a Debug build installed on $SERIAL"; exit 1; }
  $A shell am force-stop "$PACKAGE"
  # adb push copies the bytes as they are (adb shell's stdin and stdout can turn LF into CRLF on Windows);
  # adb is a Windows program, so it gets the Windows form of a Git Bash path such as /tmp/....
  $A push "$(win "$BACKUP")" /data/local/tmp/kakeibo-data.tar > /dev/null && $A shell chmod 644 /data/local/tmp/kakeibo-data.tar \
    && $A shell "run-as $PACKAGE tar xf /data/local/tmp/kakeibo-data.tar" || { echo "FAIL: could not restore $BACKUP"; exit 1; }
  $A shell rm -f /data/local/tmp/kakeibo-data.tar
  echo "restored $BACKUP on $SERIAL"; exit 0
fi

APK=${2:?usage: release_check.sh <serial> <apk> <out dir>}
OUT=${3:?usage: release_check.sh <serial> <apk> <out dir>}
[ -f "$APK" ] || { echo "FAIL: $APK not found"; exit 1; }
mkdir -p "$OUT"
PY=python

if $A shell pm path "$PACKAGE" > /dev/null 2>&1 && $A shell run-as "$PACKAGE" true > /dev/null 2>&1; then
  BACKUP=$(mktemp -t kakeibo-emulator-data.XXXXXX).tar
  # exec-out, not shell: the tar is binary and must arrive byte for byte. The database and settings only:
  # Visual Studio's fast-deployment files come back with its next deploy.
  $A exec-out run-as "$PACKAGE" tar cf - files/KakeiboApp.db3 shared_prefs > "$BACKUP" 2>/dev/null
  tar tf "$(win "$BACKUP")" --force-local > /dev/null 2>&1 \
    || { echo "FAIL: the backup of $SERIAL's data is not readable, so the app was left installed: $BACKUP"; exit 1; }
  echo "emulator data backed up: $BACKUP"
fi

$A uninstall "$PACKAGE" > /dev/null 2>&1
timeout 300 $A install "$APK" > /dev/null || { echo "FAIL: the APK did not install on $SERIAL"; exit 1; }
$A logcat -c

STEP=0
shot() { STEP=$((STEP + 1)); $A exec-out screencap -p > "$OUT/$(printf '%02d' $STEP)-$1.png"; }
dump() { $A shell uiautomator dump /sdcard/kakeibo-ui.xml > /dev/null 2>&1; $A pull /sdcard/kakeibo-ui.xml "$(win "$OUT/ui.xml")" > /dev/null 2>&1; }
# Taps the element with exactly this text; FAIL when the screen does not have it.
tap() {
  dump
  local at; at=$($PY -I "$(win "$HERE/ui_find.py")" "$(win "$OUT/ui.xml")" "$1" ${2:-})
  [ -n "$at" ] || fail "「$1」 was not on the screen"
  $A shell input tap $at
}
fail() {
  $A logcat -d > "$OUT/logcat.txt" 2>/dev/null
  shot failed
  echo "$($A logcat -d -s AndroidRuntime:E | grep -A12 "Process: $PACKAGE" | head -14)"
  echo "FAIL: $1 (step $STEP; screenshots and logcat.txt in $OUT)"; exit 1
}
# After every step: still running, and no crash of the app in logcat.
alive() {
  if $A logcat -d -s AndroidRuntime:E | grep -q "Process: $PACKAGE" || ! $A shell pidof "$PACKAGE" > /dev/null; then
    fail "the app crashed or stopped: $1"
  fi
  shot "$1"
}

# The first launch of a release build takes a few seconds; a crash in the theme or the first page shows by then.
$A shell monkey -p "$PACKAGE" -c android.intent.category.LAUNCHER 1 > /dev/null 2>&1
sleep 15
alive launch
# A fresh install shows 使い方 first.
dump
if [ -n "$($PY -I "$(win "$HERE/ui_find.py")" "$(win "$OUT/ui.xml")" スキップ)" ]; then tap スキップ; sleep 3; alive home; fi
for tab in 生活費 特別出費 設定 ホーム; do
  tap "$tab" --lowest; sleep 4; alive "tab-$tab"
done
# The add button: 「＋ 生活費をつける」, or 「＋ つける」 flicked left (生活費) and up (特別出費).
dump
if [ -n "$($PY -I "$(win "$HERE/ui_find.py")" "$(win "$OUT/ui.xml")" 生活費をつける)" ]; then
  tap 生活費をつける; sleep 3; alive entry-sheet
  tap 閉じる; sleep 3; alive entry-sheet-closed
else
  at=$($PY -I "$(win "$HERE/ui_find.py")" "$(win "$OUT/ui.xml")" つける)
  [ -n "$at" ] || fail "neither 「生活費をつける」 nor 「つける」 was on the screen"
  set -- $at
  $A shell input swipe $1 $2 $(($1 - 300)) $2 300; sleep 3; alive entry-sheet
  dump; [ -n "$($PY -I "$(win "$HERE/ui_find.py")" "$(win "$OUT/ui.xml")" 生活費をつける)" ] || fail "a flick left did not open 生活費をつける"
  tap 閉じる; sleep 3; alive entry-sheet-closed
  $A shell input swipe $1 $2 $1 $(($2 - 300)) 300; sleep 3; alive special-sheet
  dump; [ -n "$($PY -I "$(win "$HERE/ui_find.py")" "$(win "$OUT/ui.xml")" 特別出費をつける)" ] || fail "a flick up did not open 特別出費をつける"
  tap 閉じる; sleep 3; alive special-sheet-closed
fi

$A logcat -d > "$OUT/logcat.txt" 2>/dev/null
echo "PASS: $(basename "$APK") on $SERIAL: launch, the four tabs and the entry sheet with no crash; look at the screenshots in $OUT"
