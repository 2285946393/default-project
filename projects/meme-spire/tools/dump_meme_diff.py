# -*- coding: utf-8 -*-
"""打印某张表里所有"改过"的条目，用来人工挑精选梗。

用法:
    python dump_meme_diff.py cards        # 全部
    python dump_meme_diff.py cards 200 0  # 前 200 条
"""
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(HERE)
MEME_DIR = os.path.join(PROJ, "MemeSpire", "localization", "zhs")
REF_DIR = os.path.join(PROJ, "reference")

TAG_RE = re.compile(r"\[/?(?:b|i|u|s|sine|red|gold|blue|green|purple|orange|aqua|thinky_dots|font_size[^\]]*|lb|rb)\]")


def plain(s):
    return TAG_RE.sub("", s or "").replace("\\n", " ").strip()


def main():
    table = sys.argv[1]
    limit = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    skip = int(sys.argv[3]) if len(sys.argv) > 3 else 0

    meme = json.load(io.open(os.path.join(MEME_DIR, table + ".json"), encoding="utf-8"))
    ref_path = os.path.join(REF_DIR, "zhs__%s.json" % table)
    ref = json.load(io.open(ref_path, encoding="utf-8")) if os.path.exists(ref_path) else {}

    rows = []
    for k, v in meme.items():
        if k.endswith(".description"):
            continue
        old = plain(ref.get(k, ""))
        new = plain(v)
        if not old or old == new:
            continue
        rows.append((old, new))

    print("# %s：共改过 %d 条" % (table, len(rows)))
    rows = rows[skip:]
    if limit:
        rows = rows[:limit]
    for old, new in rows:
        print("%s ⇒ %s" % (old, new))


if __name__ == "__main__":
    main()
