#!/usr/bin/env bash
# Build the signed Android APK from a clean Release build and copy it to the Google Drive folder
# that Drive for desktop syncs.
#
#   bash .claude/skills/android-release/scripts/build_apk.sh [--no-copy | --copy-only] [--overwrite]
#
# Prints the path of the copy on Drive on its last line. Stops if it is not on master, if
# Repository/Constants.cs is the committed version with the dummy license key, if the APK is signed
# with another key than KAKEIBO_CERT_SHA256, or if Drive already has an APK with this versionCode
# (--overwrite replaces it).
#   --no-copy    build and check the APK, print its path, and stop before Drive (for release_check.sh)
#   --copy-only  copy the APK the last --no-copy built, checking its signature again
#   --any-branch copy a build that is not from master (the skill's release-only mode, which makes no PR)
# A build that is not copied (--no-copy) may be from any branch: the release check builds fixes on theirs.
#
# Settings come from the environment, or else from the "env" of .claude/settings.local.json (so the
# script also works when the user runs it in their own terminal):
#   KAKEIBO_DRIVE_DIR      the synced Drive folder, for example G:\My Drive\Kakeibo
#   KAKEIBO_KEYSTORE       path to the signing keystore, outside the repository
#   KAKEIBO_KEY_ALIAS      key alias in it
#   KAKEIBO_CERT_SHA256    SHA-256 of the certificate the installed app is signed with
#   KAKEIBO_STORE_PASS, KAKEIBO_KEY_PASS
#                          the passwords (optional), from the environment only: Claude Code sets them
#                          from settings.local.json; the script itself never reads them from the file.
#                          When they are not set, it asks for them in a terminal. MSBuild reads them
#                          itself through env:, so they never appear in a command line, a log or this output.
set -euo pipefail
cd "$(dirname "$0")/../../../.."   # the repository root, wherever this is run from

OVERWRITE=false
ANY_BRANCH=false
BUILD=true
COPY=true
for arg in "$@"; do
  case "$arg" in
    --overwrite) OVERWRITE=true ;;
    --no-copy) COPY=false ;;
    --copy-only) BUILD=false ;;
    --any-branch) ANY_BRANCH=true ;;
    *) echo "FAIL: unknown argument $arg (--no-copy, --copy-only, --any-branch, --overwrite)"; exit 1 ;;
  esac
done
[ "$BUILD" = true ] || [ "$COPY" = true ] || { echo "FAIL: --no-copy and --copy-only together do nothing"; exit 1; }

# Values from .claude/settings.local.json for the non-secret names the environment does not set.
# Read here, never printed. The passwords are not taken from this file (see below).
if [ -f .claude/settings.local.json ]; then
  while IFS= read -r -d '' pair; do
    name=${pair%%=*}
    case "$name" in
      KAKEIBO_DRIVE_DIR|KAKEIBO_KEYSTORE|KAKEIBO_KEY_ALIAS|KAKEIBO_CERT_SHA256) ;;
      *) continue ;;
    esac
    [ -n "${!name:-}" ] || declare -x "$name=${pair#*=}"
  done < <(python -I .claude/skills/android-release/scripts/local_env.py .claude/settings.local.json)
fi

# What goes to Drive is the merged code (unless --any-branch), always with the real Syncfusion license key.
if [ "$COPY" = true ] && [ "$ANY_BRANCH" = false ] && [ "$(git rev-parse --abbrev-ref HEAD)" != master ]; then
  echo "FAIL: not on master; copy the APK built from the merged master (or --any-branch in release-only mode)"; exit 1
fi
if git diff --quiet HEAD -- Repository/Constants.cs; then
  echo "FAIL: Repository/Constants.cs is the committed version with the dummy key; the release needs the real key in the working tree"; exit 1
fi

missing=()
for name in KAKEIBO_DRIVE_DIR KAKEIBO_KEYSTORE KAKEIBO_KEY_ALIAS KAKEIBO_CERT_SHA256; do
  [ -n "${!name:-}" ] || missing+=("$name")
done
if [ ${#missing[@]} -gt 0 ]; then
  echo "FAIL: not set: ${missing[*]} (put them in the env of .claude/settings.local.json; see the skill's One-time setup)"; exit 1
fi

# Paths may be written C:\...; check them in POSIX form and give Windows programs the Windows form.
posix() { if command -v cygpath > /dev/null; then cygpath -u "$1"; else printf '%s' "$1"; fi; }
windows() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else printf '%s' "$1"; fi; }
KEYSTORE=$(posix "$KAKEIBO_KEYSTORE")
DRIVE=$(posix "$KAKEIBO_DRIVE_DIR")
[ -f "$KEYSTORE" ] || { echo "FAIL: KAKEIBO_KEYSTORE does not point to a file"; exit 1; }
case "$(cd "$(dirname "$KEYSTORE")" && pwd)/" in
  "$(pwd)/"*) echo "FAIL: the keystore is inside the repository; keep it outside so it can never be committed"; exit 1 ;;
esac
[ -d "$DRIVE" ] || { echo "FAIL: KAKEIBO_DRIVE_DIR is not a folder: $KAKEIBO_DRIVE_DIR (is Google Drive for desktop running?)"; exit 1; }

# The same name Visual Studio's archive gives the APK: <package>_<versionCode>.apk.
PACKAGE=$(sed -n 's:.*<ApplicationId>\(.*\)</ApplicationId>.*:\1:p' KakeiboApp.csproj | head -1)
CODE=$(sed -n 's:.*<ApplicationVersion>\(.*\)</ApplicationVersion>.*:\1:p' KakeiboApp.csproj | head -1)
NAME=$(sed -n 's:.*<ApplicationDisplayVersion>\(.*\)</ApplicationDisplayVersion>.*:\1:p' KakeiboApp.csproj | head -1)
[ -n "$PACKAGE" ] && [ -n "$CODE" ] && [ -n "$NAME" ] || { echo "FAIL: could not read ApplicationId / ApplicationVersion / ApplicationDisplayVersion from KakeiboApp.csproj"; exit 1; }
TARGET="$DRIVE/${PACKAGE}_${CODE}.apk"
if [ "$COPY" = true ] && [ -e "$TARGET" ] && [ "$OVERWRITE" != true ]; then
  echo "FAIL: Drive already has ${PACKAGE}_${CODE}.apk (versionCode $CODE); pass --overwrite to replace it, or raise the version"; exit 1
fi

# The passwords: from the environment, or typed by the user. Never echoed. Only a build needs them.
for name in KAKEIBO_STORE_PASS KAKEIBO_KEY_PASS; do
  [ "$BUILD" = true ] || break
  [ -n "${!name:-}" ] && continue
  if [ "$name" = KAKEIBO_KEY_PASS ] && [ -n "${KAKEIBO_STORE_PASS:-}" ]; then
    # A PKCS12 keystore (what Visual Studio makes) has one password for the store and the key.
    export KAKEIBO_KEY_PASS=$KAKEIBO_STORE_PASS; continue
  fi
  if ! { : < /dev/tty; } 2> /dev/null; then
    echo "NEEDS_PASSWORD: KAKEIBO_STORE_PASS is not set and there is no terminal to ask in. Run this script in a terminal (Git Bash) so it can ask for the keystore password."; exit 3
  fi
  read -r -s -p "Keystore password for $(basename "$KEYSTORE"): " value < /dev/tty; echo > /dev/tty
  [ -n "$value" ] || { echo "FAIL: no password entered"; exit 1; }
  export "$name=$value"; unset value
done

# apksigner from the newest SDK build-tools, run through java: the .bat wrapper breaks on paths with
# spaces when called from Git Bash.
SDK_BUILD_TOOLS=$(ls -d "/c/Program Files (x86)/Android/android-sdk/build-tools"/*/ "${ANDROID_HOME:-/nonexistent}"/build-tools/*/ 2>/dev/null | sort -V | tail -1 || true)
APKSIGNER="${SDK_BUILD_TOOLS%/}/lib/apksigner.jar"
[ -f "$APKSIGNER" ] || { echo "FAIL: no apksigner.jar in the Android SDK build-tools; set ANDROID_HOME to the SDK"; exit 1; }
JAVA=
for candidate in "${JAVA_HOME:-/nonexistent}"/bin/java.exe "${JAVA_HOME:-/nonexistent}"/bin/java \
                 "/c/Program Files/Microsoft"/jdk-*/bin/java.exe \
                 "/c/Program Files/Android/Android Studio/jbr/bin/java.exe" \
                 java; do
  if "$candidate" -version > /dev/null 2>&1; then JAVA=$candidate; break; fi
done
[ -n "$JAVA" ] || { echo "FAIL: no working java to run apksigner; set JAVA_HOME"; exit 1; }

OUT=bin/Release/net10.0-android/publish
if [ "$BUILD" = true ]; then
# A clean build: the intermediate files too, not just the output. 2.5 was built over stale ones in obj
# (Visual Studio's design-time builds share it) and crashed at launch, its theme resources missing; the same
# code built clean was fine. The build servers are stopped first so nothing holds or rewrites obj meanwhile.
dotnet build-server shutdown > /dev/null 2>&1 || true
rm -rf obj/Release bin/Release
[ ! -e obj/Release ] || { echo "FAIL: obj/Release could not be deleted (is Visual Studio building the project?)"; exit 1; }
LOG=$(mktemp -t kakeibo-apk.XXXXXX)
# The log is kept only when the build fails, to look into it.
trap 'rm -f "$LOG"' EXIT
echo "building ${PACKAGE} ${NAME} (versionCode ${CODE}), signed with $(basename "$KEYSTORE") ..."
if ! dotnet publish KakeiboApp.csproj -f net10.0-android -c Release \
    -p:AndroidPackageFormat=apk \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$(windows "$KEYSTORE")" \
    -p:AndroidSigningKeyAlias="$KAKEIBO_KEY_ALIAS" \
    -p:AndroidSigningStorePass=env:KAKEIBO_STORE_PASS \
    -p:AndroidSigningKeyPass=env:KAKEIBO_KEY_PASS \
    > "$LOG" 2>&1; then
  # Lines about Constants.cs are left out: an error there may quote the license key.
  grep -E " error " "$LOG" | grep -v "Constants\.cs" | sort -u | head -20
  trap - EXIT
  echo "FAIL: the APK build failed (full log: $LOG)"; exit 1
fi
fi

APK=$(ls "$OUT"/*-Signed.apk 2>/dev/null | head -1)
[ -n "$APK" ] || { echo "FAIL: no signed .apk in $OUT (run with --no-copy first)"; exit 1; }
# --copy-only: the APK in the output must be this version's build, not an older one.
if [ "$BUILD" = false ]; then
  AAPT="${SDK_BUILD_TOOLS%/}/aapt.exe"; [ -x "$AAPT" ] || AAPT="${SDK_BUILD_TOOLS%/}/aapt"
  BUILT_CODE=$("$AAPT" dump badging "$(windows "$APK")" 2>/dev/null | sed -n "s/.*versionCode='\([0-9]*\)'.*/\1/p" | head -1)
  [ "$BUILT_CODE" = "$CODE" ] || { echo "FAIL: $APK is versionCode ${BUILT_CODE:-unknown}, not $CODE; build it again with --no-copy"; exit 1; }
fi

# The key the installed app was signed with: Android refuses an update signed with another key, and
# the only way round that is uninstalling, which deletes the data.
CERTS=$("$JAVA" -jar "$(windows "$APKSIGNER")" verify --print-certs "$(windows "$APK")" 2>/dev/null) \
  || { echo "FAIL: $APK does not verify"; exit 1; }
SIGNER=$(printf '%s\n' "$CERTS" | sed -n 's/^Signer #1 certificate DN: //p' | head -1)
DIGEST=$(printf '%s\n' "$CERTS" | sed -n 's/^Signer #1 certificate SHA-256 digest: //p' | head -1)
case "$SIGNER" in
  *"CN=Android Debug"*) echo "FAIL: $APK is signed with the debug key"; exit 1 ;;
esac
EXPECTED=$(printf '%s' "$KAKEIBO_CERT_SHA256" | tr 'A-F' 'a-f' | tr -d ': ')
[ -n "$DIGEST" ] || { echo "FAIL: could not read the signing certificate of $APK"; exit 1; }
if [ "$(printf '%s' "$DIGEST" | tr 'A-F' 'a-f')" != "$EXPECTED" ]; then
  echo "FAIL: $APK is signed with another key (SHA-256 $DIGEST, expected $EXPECTED); it would not install over the current app"; exit 1
fi

# Where the APK came from, written next to it by the build and checked before any copy, so what goes to
# Drive is a build of the commit at hand: not one left from another branch or from uncommitted changes.
STAMP="$OUT/built-from"
APK_SHA=$(sha256sum < "$APK" | cut -d' ' -f1)
if [ "$BUILD" = true ]; then
  TREE=clean
  [ -z "$(git status --porcelain --untracked-files=no | grep -v ' Repository/Constants.cs$' || true)" ] || TREE=dirty
  echo "$(git rev-parse HEAD) $TREE $APK_SHA" > "$STAMP"
fi
if [ "$COPY" = true ]; then
  read -r STAMP_HEAD STAMP_TREE STAMP_SHA < "$STAMP" 2>/dev/null || { echo "FAIL: $APK has no build stamp; build it with --no-copy"; exit 1; }
  [ "$STAMP_HEAD" = "$(git rev-parse HEAD)" ] || { echo "FAIL: $APK was built from $STAMP_HEAD, not the current commit; build it again"; exit 1; }
  [ "$STAMP_TREE" = clean ] || { echo "FAIL: $APK was built with uncommitted changes; commit them and build again"; exit 1; }
  [ "$STAMP_SHA" = "$APK_SHA" ] || { echo "FAIL: $APK changed after it was built; build it again"; exit 1; }
  if [ "$ANY_BRANCH" = false ] && [ "$STAMP_HEAD" != "$(git rev-parse origin/master 2>/dev/null)" ]; then
    echo "FAIL: $APK is not a build of the merged origin/master; pull master and build again (or --any-branch in release-only mode)"; exit 1
  fi
fi

if [ "$COPY" = false ]; then
  echo "APK built: $(du -h "$APK" | cut -f1), signed by $SIGNER (SHA-256 $DIGEST); not copied to Drive (--no-copy)"
  echo "$(windows "$APK")"
  exit 0
fi

cp -f "$APK" "$TARGET.part"
mv -f "$TARGET.part" "$TARGET"
[ "$(sha256sum < "$APK")" = "$(sha256sum < "$TARGET")" ] || { echo "FAIL: the copy on Drive differs from the build"; exit 1; }
if command -v tasklist > /dev/null && ! tasklist //FI "IMAGENAME eq GoogleDriveFS.exe" 2>/dev/null | grep -qi GoogleDriveFS; then
  echo "WARNING: Google Drive for desktop (GoogleDriveFS.exe) is not running, so the file is not being uploaded yet"
fi
echo "APK built: $(du -h "$APK" | cut -f1), signed by $SIGNER (SHA-256 $DIGEST)"
echo "copied to Drive: $(windows "$TARGET")"
echo "$(windows "$TARGET")"
