"""Raise the app's version in KakeiboApp.csproj.

    python -I .claude/skills/android-release/scripts/bump_version.py [--dry-run] [--code-only | --version X.Y]

By default versionCode (ApplicationVersion) goes up by 1 and versionName (ApplicationDisplayVersion)
goes up by one minor step (2.0 -> 2.1). --code-only keeps versionName; --version sets it. A higher
versionName shows 「使い方」 once after the update (OnboardingPolicy), a versionCode alone does not.
Keeps the file's BOM and line endings, and refuses a versionName that does not go up.
"""
import argparse
import pathlib
import re
import sys

CSPROJ = pathlib.Path(__file__).resolve().parents[4] / "KakeiboApp.csproj"
NAME = re.compile(r"<ApplicationDisplayVersion>([^<]*)</ApplicationDisplayVersion>")
CODE = re.compile(r"<ApplicationVersion>([^<]*)</ApplicationVersion>")


def parts(version):
    numbers = [int(p) for p in version.strip().split(".")]
    return (numbers + [0, 0, 0])[:3]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    group = parser.add_mutually_exclusive_group()
    group.add_argument("--code-only", action="store_true")
    group.add_argument("--version")
    args = parser.parse_args()

    raw = CSPROJ.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw[3:].decode("utf-8") if bom else raw.decode("utf-8")

    name_match, code_match = NAME.search(text), CODE.search(text)
    if not name_match or not code_match:
        sys.exit("FAIL: no ApplicationDisplayVersion / ApplicationVersion in KakeiboApp.csproj")
    old_name, old_code = name_match.group(1), int(code_match.group(1))

    if args.code_only:
        new_name = old_name
    elif args.version:
        new_name = args.version.strip()
        if not re.fullmatch(r"\d+(\.\d+){1,2}", new_name) or parts(new_name) <= parts(old_name):
            sys.exit(f"FAIL: versionName {new_name} is not a version above {old_name}")
    else:
        major, minor, _ = parts(old_name)
        new_name = f"{major}.{minor + 1}"
    new_code = old_code + 1

    print(f"versionName {old_name} -> {new_name}, versionCode {old_code} -> {new_code}")
    if args.dry_run:
        return

    text = NAME.sub(f"<ApplicationDisplayVersion>{new_name}</ApplicationDisplayVersion>", text, count=1)
    text = CODE.sub(f"<ApplicationVersion>{new_code}</ApplicationVersion>", text, count=1)
    CSPROJ.write_bytes((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))


if __name__ == "__main__":
    main()
