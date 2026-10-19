"""Print the centre "x y" of the element with exactly this text or description in a uiautomator dump.

    python -I ui_find.py <dump.xml> <text> [--lowest]

--lowest picks the match lowest on the screen, for the tab bar ("生活費" is also a card's heading).
Prints nothing when there is no match.
"""
import re
import sys

dump, text = sys.argv[1], sys.argv[2]
lowest = "--lowest" in sys.argv[3:]
with open(dump, encoding="utf-8") as f:
    xml = f.read()

matches = []
for node in re.findall(r"<node [^>]*>", xml):
    if re.search(r'(?:text|content-desc)="' + re.escape(text) + '"', node):
        bounds = re.search(r'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"', node)
        if bounds:
            x1, y1, x2, y2 = map(int, bounds.groups())
            matches.append(((x1 + x2) // 2, (y1 + y2) // 2))
if matches:
    x, y = max(matches, key=lambda m: m[1]) if lowest else matches[0]
    print(x, y)
