"""Print the non-secret KAKEIBO_* values in the "env" of a Claude Code settings file, as NAME=value, each ended by NUL.

    python -I local_env.py .claude/settings.local.json

build_apk.sh reads them so it also works outside Claude Code. Only the names below are printed: the
passwords never go through this pipe (build_apk.sh takes them from its environment or asks for them).
"""
import json
import sys

NAMES = ("KAKEIBO_DRIVE_DIR", "KAKEIBO_KEYSTORE", "KAKEIBO_KEY_ALIAS")

with open(sys.argv[1], encoding="utf-8-sig") as f:
    env = json.load(f).get("env") or {}
for name in NAMES:
    value = env.get(name)
    if isinstance(value, str) and "\0" not in value:
        sys.stdout.write(name + "=" + value + "\0")
