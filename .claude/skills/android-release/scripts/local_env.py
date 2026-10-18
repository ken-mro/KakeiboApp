"""Print the KAKEIBO_* values in the "env" of a Claude Code settings file, as NAME=value, each ended by NUL.

    python -I local_env.py .claude/settings.local.json

build_apk.sh reads them so it also works outside Claude Code. The values are only printed to that pipe.
"""
import json
import sys

with open(sys.argv[1], encoding="utf-8-sig") as f:
    env = json.load(f).get("env") or {}
for name, value in env.items():
    if name.startswith("KAKEIBO_") and isinstance(value, str):
        sys.stdout.write(name + "=" + value + "\0")
